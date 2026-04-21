using System.Text.Json.Serialization;

namespace SkillServer.Models;

/// <summary>
/// NetClaw manifest.json format for backwards compatibility.
/// Served at /manifest.json
/// </summary>
public sealed record NetclawManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("feedType")]
    public string FeedType { get; init; } = "private";

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("skills")]
    public required IReadOnlyList<NetclawSkillEntry> Skills { get; init; }
}

/// <summary>
/// Individual skill entry in the NetClaw manifest.
/// </summary>
public sealed record NetclawSkillEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("minimumDaemonVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MinimumDaemonVersion { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("files")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<NetclawFileEntry>? Files { get; init; }
}

/// <summary>
/// Resource file entry in the NetClaw manifest.
/// </summary>
public sealed record NetclawFileEntry
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}
