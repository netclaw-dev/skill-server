// -----------------------------------------------------------------------
// <copyright file="NativeManifest.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace SkillServer.Models;

public sealed record NativeManifestLink
{
    [JsonPropertyName("href")]
    public required string Href { get; init; }
}

public sealed record NativeManifestSelfLinks
{
    [JsonPropertyName("self")]
    public required NativeManifestLink Self { get; init; }
}

public sealed record NativeRootManifestLinks
{
    [JsonPropertyName("self")]
    public required NativeManifestLink Self { get; init; }

    [JsonPropertyName("rfcSkills")]
    public required NativeManifestLink RfcSkills { get; init; }

    [JsonPropertyName("skills")]
    public required NativeManifestLink Skills { get; init; }
}

public sealed record NativeRootManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://schemas.netclaw.dev/skillserver/manifest/0.1.0";

    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("links")]
    public required NativeRootManifestLinks Links { get; init; }
}

public sealed record NativeManifestPageLink
{
    [JsonPropertyName("range")]
    public required string Range { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }
}

public sealed record NativeSkillCollectionIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "skill-index";

    [JsonPropertyName("links")]
    public required NativeManifestSelfLinks Links { get; init; }

    [JsonPropertyName("pages")]
    public required IReadOnlyList<NativeManifestPageLink> Pages { get; init; }
}

public sealed record NativeVersionRange
{
    [JsonPropertyName("min")]
    public required string Min { get; init; }

    [JsonPropertyName("max")]
    public required string Max { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }
}

public sealed record NativeSkillPageItem
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; init; }

    [JsonPropertyName("versionRange")]
    public required NativeVersionRange VersionRange { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }
}

public sealed record NativeSkillCollectionPage
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "skill-page";

    [JsonPropertyName("range")]
    public required string Range { get; init; }

    [JsonPropertyName("items")]
    public required IReadOnlyList<NativeSkillPageItem> Items { get; init; }

    [JsonPropertyName("links")]
    public required NativeManifestSelfLinks Links { get; init; }
}

public sealed record NativeSkillVersionLink
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("publishedAt")]
    public required DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }
}

public sealed record NativeSkillIdentityIndex
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "skill";

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; init; }

    [JsonPropertyName("versions")]
    public required IReadOnlyList<NativeSkillVersionLink> Versions { get; init; }

    [JsonPropertyName("links")]
    public required NativeManifestSelfLinks Links { get; init; }
}

public sealed record NativeSkillArtifact
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }
}

public sealed record NativeSubagentRoute
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }
}

public sealed record NativeSkillVersionDetail
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "skill-version";

    [JsonPropertyName("artifact")]
    public required NativeSkillArtifact Artifact { get; init; }

    [JsonPropertyName("routesToSubagent")]
    public NativeSubagentRoute? RoutesToSubagent { get; init; }

    [JsonPropertyName("links")]
    public required NativeManifestSelfLinks Links { get; init; }
}
