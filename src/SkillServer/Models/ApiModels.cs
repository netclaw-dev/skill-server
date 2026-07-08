// -----------------------------------------------------------------------
// <copyright file="ApiModels.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace SkillServer.Models;

/// <summary>
/// Request to upload a new skill version.
/// </summary>
public sealed record SkillUploadRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }
}

/// <summary>
/// Response after successfully uploading a skill.
/// </summary>
public sealed record SkillUploadResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

public sealed record SkillResourceUploadMetadata
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("unixMode")]
    public int? UnixMode { get; init; }
}

public sealed record SubAgentUploadResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

/// <summary>
/// Summary of a skill (all versions).
/// </summary>
public sealed record SkillSummary
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("versionCount")]
    public required int VersionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Summary of a specific skill version.
/// </summary>
public sealed record SkillVersionSummary
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("publishedAt")]
    public required DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("isLatest")]
    public required bool IsLatest { get; init; }

    [JsonPropertyName("fileCount")]
    public required int FileCount { get; init; }
}

public sealed record SkillResourceSummary
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("unixMode")]
    public int? UnixMode { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("previewable")]
    public required bool Previewable { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }
}

public sealed record SubAgentSummary
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; init; }

    [JsonPropertyName("versionCount")]
    public required int VersionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SubAgentVersionSummary
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = SubAgentTypes.AgentMd;

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("modelRole")]
    public required string ModelRole { get; init; }

    [JsonPropertyName("timeoutSeconds")]
    public required int TimeoutSeconds { get; init; }

    [JsonPropertyName("prefillTimeoutSeconds")]
    public int? PrefillTimeoutSeconds { get; init; }

    [JsonPropertyName("visibility")]
    public required string Visibility { get; init; }

    [JsonPropertyName("emitStructuredFindings")]
    public required bool EmitStructuredFindings { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("publishedAt")]
    public required DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("isLatest")]
    public required bool IsLatest { get; init; }
}

/// <summary>
/// Standard error response.
/// </summary>
public sealed record ErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

public sealed record CheckUpdateRequestItem
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

public sealed record CheckUpdateResponseItem
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("currentVersion")]
    public required string CurrentVersion { get; init; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; init; }

    [JsonPropertyName("latestDigest")]
    public required string LatestDigest { get; init; }

    [JsonPropertyName("latestPublishedAt")]
    public required DateTimeOffset LatestPublishedAt { get; init; }

    [JsonPropertyName("hasUpdate")]
    public required bool HasUpdate { get; init; }
}

public sealed record CreateApiKeyRequest
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record CreateApiKeyResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record ApiKeySummary
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record AppInfoResponse
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("assemblyVersion")]
    public required string AssemblyVersion { get; init; }
}

/// <summary>
/// Response from the health check endpoint.
/// </summary>
public sealed record HealthResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}
