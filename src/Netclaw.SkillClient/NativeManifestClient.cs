// -----------------------------------------------------------------------
// <copyright file="NativeManifestClient.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net.Http.Json;

namespace Netclaw.SkillClient;

public sealed partial class SkillServerClient
{
    private static readonly string[] SupportedVersions = ["v1"];
    private NativeVersionLinks? _resolvedLinks;

    public async Task<NativeRootManifest?> GetManifestAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync(
            "/manifest.json",
            SkillServerClientJsonContext.Default.NativeRootManifest,
            ct);
    }

    /// <summary>
    /// Resolves the best supported API version from the manifest.
    /// Client knows which versions it supports, picks the most recent
    /// version the server also supports.
    /// </summary>
    public async Task<NativeVersionLinks> ResolveVersionAsync(CancellationToken ct = default)
    {
        if (_resolvedLinks is not null)
            return _resolvedLinks;

        var manifest = await GetManifestAsync(ct)
            ?? throw new InvalidOperationException("Failed to fetch manifest from server.");

        foreach (var version in SupportedVersions.Reverse())
        {
            if (manifest.Versions.TryGetValue(version, out var links))
            {
                _resolvedLinks = links;
                return links;
            }
        }

        throw new NotSupportedException(
            $"Server supports [{string.Join(", ", manifest.Versions.Keys)}] " +
            $"but client supports [{string.Join(", ", SupportedVersions)}].");
    }

    public Task<NativeSkillCollectionIndex?> GetNativeSkillIndexAsync(CancellationToken ct = default)
    {
        return GetNativeSkillIndexByHrefAsync("/skills/v1/index.json", ct);
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
            $"/skills/v1/{Uri.EscapeDataString(name)}/index.json",
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
            $"/skills/v1/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}.json",
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
        return GetNativeSubAgentIndexByHrefAsync("/subagents/v1/index.json", ct);
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
            $"/subagents/v1/{Uri.EscapeDataString(name)}/index.json",
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
            $"/subagents/v1/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}.json",
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