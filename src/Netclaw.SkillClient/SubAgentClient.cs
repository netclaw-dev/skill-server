// -----------------------------------------------------------------------
// <copyright file="SubAgentClient.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Json;

namespace Netclaw.SkillClient;

public sealed partial class SkillServerClient
{
    public async Task<IReadOnlyList<SubAgentSummary>> ListSubAgentsAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync(
            "subagents",
            SkillServerClientJsonContext.Default.IReadOnlyListSubAgentSummary,
            ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SubAgentVersionSummary>> GetSubAgentVersionsAsync(
        string name,
        CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync(
            $"subagents/{Uri.EscapeDataString(name)}",
            SkillServerClientJsonContext.Default.IReadOnlyListSubAgentVersionSummary,
            ct);
        return result ?? [];
    }

    public async Task<SubAgentVersionSummary?> GetSubAgentVersionAsync(
        string name,
        string version,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            $"subagents/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}",
            SkillServerClientJsonContext.Default.SubAgentVersionSummary,
            ct);
    }

    public async Task<Stream> GetSubAgentFileAsync(string name, string version, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"subagents/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}/agent.md",
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<string> GetSubAgentFileAsStringAsync(
        string name,
        string version,
        CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"subagents/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}/agent.md",
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<SubAgentUploadResponse> UploadSubAgentAsync(
        string name,
        string version,
        Stream agentMdContent,
        CancellationToken ct = default)
    {
        using var response = await PostSubAgentAsync(name, version, agentMdContent, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.SubAgentUploadResponse,
            ct))!;
    }

    public async Task<SubAgentUploadResponse?> UploadSubAgentIfNotExistsAsync(
        string name,
        string version,
        Stream agentMdContent,
        CancellationToken ct = default)
    {
        using var response = await PostSubAgentAsync(name, version, agentMdContent, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
            return null;

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(
            SkillServerClientJsonContext.Default.SubAgentUploadResponse,
            ct))!;
    }

    public async Task DeleteSubAgentVersionAsync(string name, string version, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"subagents/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}",
            ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> PostSubAgentAsync(
        string name,
        string version,
        Stream agentMdContent,
        CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(version), "version");
        content.Add(new StreamContent(agentMdContent), "file", "agent.md");

        return await _httpClient.PostAsync("subagents", content, ct);
    }
}
