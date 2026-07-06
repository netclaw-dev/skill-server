// -----------------------------------------------------------------------
// <copyright file="NativeManifestModels.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace Netclaw.SkillClient;

public sealed record NativeManifestLink
{
    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeManifestSelfLinks
{
    [JsonPropertyName("self")]
    public NativeManifestLink Self { get; init; } = new();
}

public sealed record NativeVersionLinks
{
    [JsonPropertyName("self")]
    public NativeManifestLink Self { get; init; } = new();

    [JsonPropertyName("skills")]
    public NativeManifestLink Skills { get; init; } = new();

    [JsonPropertyName("subagents")]
    public NativeManifestLink SubAgents { get; init; } = new();

    [JsonPropertyName("skillSearch")]
    public NativeManifestLink SkillSearch { get; init; } = new();

    [JsonPropertyName("subagentSearch")]
    public NativeManifestLink SubAgentSearch { get; init; } = new();
}

public sealed record NativeRootManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "";

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = "";

    [JsonPropertyName("versions")]
    public Dictionary<string, NativeVersionLinks> Versions { get; init; } = new();
}

public sealed record NativeManifestPageLink
{
    [JsonPropertyName("range")]
    public string Range { get; init; } = "";

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSkillCollectionIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();

    [JsonPropertyName("pages")]
    public IReadOnlyList<NativeManifestPageLink> Pages { get; init; } = [];
}

public sealed record NativeSubAgentCollectionIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();

    [JsonPropertyName("pages")]
    public IReadOnlyList<NativeManifestPageLink> Pages { get; init; } = [];
}

public sealed record NativeVersionRange
{
    [JsonPropertyName("min")]
    public string Min { get; init; } = "";

    [JsonPropertyName("max")]
    public string Max { get; init; } = "";

    [JsonPropertyName("count")]
    public int Count { get; init; }
}

public sealed record NativeSkillPageItem
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("versionRange")]
    public NativeVersionRange VersionRange { get; init; } = new();

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSubAgentPageItem
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("versionRange")]
    public NativeVersionRange VersionRange { get; init; } = new();

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSkillCollectionPage
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("range")]
    public string Range { get; init; } = "";

    [JsonPropertyName("items")]
    public IReadOnlyList<NativeSkillPageItem> Items { get; init; } = [];

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}

public sealed record NativeSubAgentCollectionPage
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("range")]
    public string Range { get; init; } = "";

    [JsonPropertyName("items")]
    public IReadOnlyList<NativeSubAgentPageItem> Items { get; init; } = [];

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}

public sealed record NativeSkillVersionLink
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSubAgentVersionLink
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSkillIdentityIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("versions")]
    public IReadOnlyList<NativeSkillVersionLink> Versions { get; init; } = [];

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}

public sealed record NativeSubAgentIdentityIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("versions")]
    public IReadOnlyList<NativeSubAgentVersionLink> Versions { get; init; } = [];

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}

public sealed record NativeSkillArtifact
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";
}

public sealed record NativeSubAgentRoute
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("href")]
    public string Href { get; init; } = "";
}

public sealed record NativeSkillVersionDetail
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("artifact")]
    public NativeSkillArtifact Artifact { get; init; } = new();

    [JsonPropertyName("routesToSubagent")]
    public NativeSubAgentRoute? RoutesToSubagent { get; init; }

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}

public sealed record NativeSubAgentVersionDetail
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";

    [JsonPropertyName("links")]
    public NativeManifestSelfLinks Links { get; init; } = new();
}
