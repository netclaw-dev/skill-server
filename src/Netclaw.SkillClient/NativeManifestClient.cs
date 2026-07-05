// -----------------------------------------------------------------------
// <copyright file="NativeManifestClient.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net.Http.Json;

namespace Netclaw.SkillClient;

public sealed partial class SkillServerClient
{
    public async Task<NativeRootManifest?> GetManifestAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            Api("manifest.json"),
            SkillServerClientJsonContext.Default.NativeRootManifest,
            ct);
    }

    public Task<NativeSkillCollectionIndex?> GetNativeSkillIndexAsync(CancellationToken ct = default)
    {
        return GetNativeSkillIndexByHrefAsync(Api("manifest/skills/index.json"), ct);
    }

    public Task<NativeSkillCollectionIndex?> GetNativeSkillIndexAsync(
        NativeManifestLink link,
        CancellationToken ct = default)
    {
        return GetNativeSkillIndexByHrefAsync(ResolveHref(link.Href), ct);
    }

    public async Task<NativeSkillCollectionIndex?> GetNativeSkillIndexByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSkillCollectionIndex,
            ct);
    }

    public Task<NativeSkillCollectionPage?> GetNativeSkillPageAsync(
        NativeManifestPageLink link,
        CancellationToken ct = default)
    {
        return GetNativeSkillPageAsync(ResolveHref(link.Href), ct);
    }

    public async Task<NativeSkillCollectionPage?> GetNativeSkillPageAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSkillCollectionPage,
            ct);
    }

    public Task<NativeSkillIdentityIndex?> GetNativeSkillIdentityAsync(
        NativeSkillPageItem item,
        CancellationToken ct = default)
    {
        return GetNativeSkillIdentityByHrefAsync(ResolveHref(item.Href), ct);
    }

    public Task<NativeSkillIdentityIndex?> GetNativeSkillIdentityAsync(string name, CancellationToken ct = default)
    {
        return GetNativeSkillIdentityByHrefAsync(
            $"manifest/skills/{Uri.EscapeDataString(name)}/index.json",
            ct);
    }

    public async Task<NativeSkillIdentityIndex?> GetNativeSkillIdentityByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSkillIdentityIndex,
            ct);
    }

    public Task<NativeSkillVersionDetail?> GetNativeSkillVersionAsync(
        NativeSkillVersionLink link,
        CancellationToken ct = default)
    {
        return GetNativeSkillVersionByHrefAsync(ResolveHref(link.Href), ct);
    }

    public Task<NativeSkillVersionDetail?> GetNativeSkillVersionAsync(
        string name,
        string version,
        CancellationToken ct = default)
    {
        return GetNativeSkillVersionByHrefAsync(
            $"manifest/skills/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}.json",
            ct);
    }

    public async Task<NativeSkillVersionDetail?> GetNativeSkillVersionByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSkillVersionDetail,
            ct);
    }

    public Task<NativeSubAgentCollectionIndex?> GetNativeSubAgentIndexAsync(CancellationToken ct = default)
    {
        return GetNativeSubAgentIndexByHrefAsync(Api("manifest/subagents/index.json"), ct);
    }

    public Task<NativeSubAgentCollectionIndex?> GetNativeSubAgentIndexAsync(
        NativeManifestLink link,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentIndexByHrefAsync(ResolveHref(link.Href), ct);
    }

    public async Task<NativeSubAgentCollectionIndex?> GetNativeSubAgentIndexByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSubAgentCollectionIndex,
            ct);
    }

    public Task<NativeSubAgentCollectionPage?> GetNativeSubAgentPageAsync(
        NativeManifestPageLink link,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentPageAsync(ResolveHref(link.Href), ct);
    }

    public async Task<NativeSubAgentCollectionPage?> GetNativeSubAgentPageAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSubAgentCollectionPage,
            ct);
    }

    public Task<NativeSubAgentIdentityIndex?> GetNativeSubAgentIdentityAsync(
        NativeSubAgentPageItem item,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentIdentityByHrefAsync(ResolveHref(item.Href), ct);
    }

    public Task<NativeSubAgentIdentityIndex?> GetNativeSubAgentIdentityAsync(
        string name,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentIdentityByHrefAsync(
            $"manifest/subagents/{Uri.EscapeDataString(name)}/index.json",
            ct);
    }

    public async Task<NativeSubAgentIdentityIndex?> GetNativeSubAgentIdentityByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSubAgentIdentityIndex,
            ct);
    }

    public Task<NativeSubAgentVersionDetail?> GetNativeSubAgentVersionAsync(
        NativeSubAgentVersionLink link,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentVersionByHrefAsync(ResolveHref(link.Href), ct);
    }

    public Task<NativeSubAgentVersionDetail?> GetNativeSubAgentVersionAsync(
        string name,
        string version,
        CancellationToken ct = default)
    {
        return GetNativeSubAgentVersionByHrefAsync(
            $"manifest/subagents/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}.json",
            ct);
    }

    public async Task<NativeSubAgentVersionDetail?> GetNativeSubAgentVersionByHrefAsync(
        string href,
        CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            ResolveHref(href),
            SkillServerClientJsonContext.Default.NativeSubAgentVersionDetail,
            ct);
    }

    /*
     * Resolve an href: if it already starts with "/" it's an absolute path from the server
     * root (as returned by manifest links), otherwise prepend the API base.
     */
    private string ResolveHref(string href)
    {
        return href.StartsWith('/') ? href : Api(href);
    }
}