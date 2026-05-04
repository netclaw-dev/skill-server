// -----------------------------------------------------------------------
// <copyright file="Models.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace Netclaw.SkillClient;

/// <summary>
/// RFC-compliant skill index.
/// </summary>
public sealed record RfcSkillIndex
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "";

    [JsonPropertyName("skills")]
    public IReadOnlyList<RfcSkillEntry> Skills { get; init; } = [];
}

/// <summary>
/// RFC skill entry.
/// </summary>
public sealed record RfcSkillEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("resources")]
    public IReadOnlyList<RfcResourceEntry>? Resources { get; init; }
}

/// <summary>
/// RFC resource entry.
/// </summary>
public sealed record RfcResourceEntry
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("digest")]
    public string Digest { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";
}

/// <summary>
/// Skill summary.
/// </summary>
public sealed record SkillSummary
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("versionCount")]
    public int VersionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Skill version summary.
/// </summary>
public sealed record SkillVersionSummary
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("isLatest")]
    public bool IsLatest { get; init; }

    [JsonPropertyName("fileCount")]
    public int FileCount { get; init; }
}

public sealed record CheckUpdateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";
}

public sealed record CheckUpdateResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; init; } = "";

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = "";

    [JsonPropertyName("latestDigest")]
    public string LatestDigest { get; init; } = "";

    [JsonPropertyName("latestPublishedAt")]
    public DateTimeOffset LatestPublishedAt { get; init; }

    [JsonPropertyName("hasUpdate")]
    public bool HasUpdate { get; init; }
}

public sealed record SkillUploadResponse
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

public sealed record CreateApiKeyResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = "";

    [JsonPropertyName("key")]
    public string Key { get; init; } = "";

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record ApiKeySummary
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = "";

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record CreateApiKeyRequest
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = "";

    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
}

/// <summary>
/// JSON serialization context for AOT support.
/// </summary>
[JsonSerializable(typeof(RfcSkillIndex))]
[JsonSerializable(typeof(IReadOnlyList<SkillSummary>))]
[JsonSerializable(typeof(IReadOnlyList<SkillVersionSummary>))]
[JsonSerializable(typeof(SkillVersionSummary))]
[JsonSerializable(typeof(SkillUploadResponse))]
[JsonSerializable(typeof(CreateApiKeyResponse))]
[JsonSerializable(typeof(ApiKeySummary))]
[JsonSerializable(typeof(IReadOnlyList<ApiKeySummary>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateRequest>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateResponse>))]
[JsonSerializable(typeof(CreateApiKeyRequest))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class SkillServerClientJsonContext : JsonSerializerContext;
