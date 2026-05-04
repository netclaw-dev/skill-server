// -----------------------------------------------------------------------
// <copyright file="SkillServerClient.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Netclaw.SkillClient;

/// <summary>
/// Client for consuming SkillServer APIs.
/// </summary>
public sealed class SkillServerClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SkillServerClient(string serverUrl, string? apiKey = null)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/") };
        _ownsHttpClient = true;
        if (!string.IsNullOrEmpty(apiKey))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = SkillServerClientJsonContext.Default
        };
    }

    public SkillServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = SkillServerClientJsonContext.Default
        };
    }

    /// <summary>
    /// Gets the RFC-compliant skill index per Cloudflare Agent Skills Discovery RFC v0.2.0.
    /// </summary>
    public async Task<RfcSkillIndex?> GetRfcIndexAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ".well-known/agent-skills/index.json",
            SkillServerClientJsonContext.Default.RfcSkillIndex,
            ct);
    }

    /// <summary>
    /// Lists all skills with optional pagination.
    /// </summary>
    public async Task<IReadOnlyList<SkillSummary>> ListSkillsAsync(
        int? skip = null,
        int? take = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (skip.HasValue) query.Add($"skip={skip.Value}");
        if (take.HasValue) query.Add($"take={take.Value}");

        var url = query.Count > 0 ? $"skills?{string.Join("&", query)}" : "skills";

        var result = await _httpClient.GetFromJsonAsync(
            url,
            SkillServerClientJsonContext.Default.IReadOnlyListSkillSummary,
            ct);
        return result ?? [];
    }

    /// <summary>
    /// Gets all versions of a skill.
    /// </summary>
    public async Task<IReadOnlyList<SkillVersionSummary>> GetSkillVersionsAsync(string name, CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync(
            $"skills/{Uri.EscapeDataString(name)}",
            SkillServerClientJsonContext.Default.IReadOnlyListSkillVersionSummary,
            ct);
        return result ?? [];
    }

    /// <summary>
    /// Gets a specific skill version.
    /// </summary>
    public async Task<SkillVersionSummary?> GetVersionAsync(string name, string version, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            $"skills/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}",
            SkillServerClientJsonContext.Default.SkillVersionSummary,
            ct);
    }

    /// <summary>
    /// Downloads a skill file.
    /// </summary>
    public async Task<Stream> GetSkillFileAsync(string name, string version, string path = "SKILL.md", CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"skills/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}/{path}",
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    /// <summary>
    /// Downloads a skill file as a string.
    /// </summary>
    public async Task<string> GetSkillFileAsStringAsync(string name, string version, string path = "SKILL.md", CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"skills/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}/{path}",
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Downloads a blob by digest.
    /// </summary>
    public async Task<Stream> GetBlobAsync(string digest, CancellationToken ct = default)
    {
        var normalizedDigest = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..]
            : digest;

        var response = await _httpClient.GetAsync($"blobs/sha256/{normalizedDigest}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    /// <summary>
    /// Verifies a skill file matches its expected digest.
    /// </summary>
    public async Task<bool> VerifyDigestAsync(string name, string version, string expectedDigest, CancellationToken ct = default)
    {
        var expectedHex = expectedDigest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? expectedDigest[7..].ToLowerInvariant()
            : expectedDigest.ToLowerInvariant();

        await using var stream = await GetSkillFileAsync(name, version, "SKILL.md", ct);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        var actualHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return actualHex == expectedHex;
    }

    /// <summary>
    /// Searches skills using full-text search.
    /// </summary>
    public async Task<IReadOnlyList<SkillSummary>> SearchSkillsAsync(
        string query, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var queryParams = new List<string> { $"q={Uri.EscapeDataString(query)}" };
        if (skip.HasValue) queryParams.Add($"skip={skip.Value}");
        if (take.HasValue) queryParams.Add($"take={take.Value}");

        var url = $"skills?{string.Join("&", queryParams)}";
        var result = await _httpClient.GetFromJsonAsync(url,
            SkillServerClientJsonContext.Default.IReadOnlyListSkillSummary, ct);
        return result ?? [];
    }

    /// <summary>
    /// Gets the latest version of a skill by name.
    /// </summary>
    public async Task<SkillVersionSummary?> GetLatestVersionAsync(string name, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            $"skills/{Uri.EscapeDataString(name)}/latest",
            SkillServerClientJsonContext.Default.SkillVersionSummary, ct);
    }

    /// <summary>
    /// Checks if any of the specified skills have newer versions available.
    /// </summary>
    public async Task<IReadOnlyList<CheckUpdateResponse>> CheckUpdatesAsync(
        IReadOnlyList<CheckUpdateRequest> items, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("skills/check-updates", items,
            SkillServerClientJsonContext.Default.IReadOnlyListCheckUpdateRequest, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.IReadOnlyListCheckUpdateResponse, ct);
        return result ?? [];
    }

    public async Task<SkillUploadResponse> UploadSkillAsync(
        string name, string version, Stream skillMdContent, string? category = null,
        CancellationToken ct = default)
    {
        return await UploadSkillWithResourcesAsync(name, version, skillMdContent, [], category, ct);
    }

    public async Task<SkillUploadResponse> UploadSkillWithResourcesAsync(
        string name, string version, Stream skillMdContent,
        IReadOnlyList<(string RelativePath, Stream Content)> resources,
        string? category = null, CancellationToken ct = default)
    {
        using var response = await PostSkillAsync(name, version, skillMdContent, resources, category, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.SkillUploadResponse, ct))!;
    }

    public async Task<SkillUploadResponse?> UploadSkillIfNotExistsAsync(
        string name, string version, Stream skillMdContent,
        IReadOnlyList<(string RelativePath, Stream Content)> resources,
        string? category = null, CancellationToken ct = default)
    {
        using var response = await PostSkillAsync(name, version, skillMdContent, resources, category, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
            return null;

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.SkillUploadResponse, ct))!;
    }

    private async Task<HttpResponseMessage> PostSkillAsync(
        string name, string version, Stream skillMdContent,
        IReadOnlyList<(string RelativePath, Stream Content)> resources,
        string? category, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(version), "version");
        if (category is not null)
            content.Add(new StringContent(category), "category");
        content.Add(new StreamContent(skillMdContent), "file", "SKILL.md");

        foreach (var (relativePath, resourceStream) in resources)
            content.Add(new StreamContent(resourceStream), "resources", relativePath);

        return await _httpClient.PostAsync("skills", content, ct);
    }

    public async Task DeleteVersionAsync(string name, string version, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"skills/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(
        string label, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
    {
        var request = new CreateApiKeyRequest { Label = label, ExpiresAt = expiresAt };
        var response = await _httpClient.PostAsJsonAsync("api-keys",
            request, SkillServerClientJsonContext.Default.CreateApiKeyRequest, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.CreateApiKeyResponse, ct))!;
    }

    public async Task<IReadOnlyList<ApiKeySummary>> ListApiKeysAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync("api-keys",
            SkillServerClientJsonContext.Default.IReadOnlyListApiKeySummary, ct);
        return result ?? [];
    }

    public async Task DeleteApiKeyAsync(long id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api-keys/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
