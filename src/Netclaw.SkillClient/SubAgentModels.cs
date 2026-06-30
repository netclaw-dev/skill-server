// -----------------------------------------------------------------------
// <copyright file="SubAgentModels.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace Netclaw.SkillClient;

public static class SubAgentArtifactTypes
{
    public const string AgentMd = "agent-md";
}

public sealed record SubAgentUploadResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";
}

public sealed record SubAgentSummary
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("versionCount")]
    public int VersionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SubAgentVersionSummary
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = SubAgentArtifactTypes.AgentMd;

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("modelRole")]
    public string ModelRole { get; init; } = "";

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; }

    [JsonPropertyName("prefillTimeoutSeconds")]
    public int? PrefillTimeoutSeconds { get; init; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; init; } = "";

    [JsonPropertyName("emitStructuredFindings")]
    public bool EmitStructuredFindings { get; init; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("isLatest")]
    public bool IsLatest { get; init; }
}

public sealed record VerifiedArtifactDownload
{
    public required string Url { get; init; }
    public required string Digest { get; init; }
    public required long SizeBytes { get; init; }
}
