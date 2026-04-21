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

    public SkillServerClient(string serverUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/") };
        _ownsHttpClient = true;
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

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
