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
