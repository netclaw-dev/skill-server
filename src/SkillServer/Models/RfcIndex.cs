// -----------------------------------------------------------------------
// <copyright file="RfcIndex.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace SkillServer.Models;

/// <summary>
/// Cloudflare Agent Skills Discovery RFC v0.2.0 index format.
/// Served at /.well-known/agent-skills/index.json
/// </summary>
public sealed record RfcSkillIndex
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://schemas.agentskills.io/discovery/0.2.0/schema.json";

    [JsonPropertyName("skills")]
    public required IReadOnlyList<RfcSkillEntry> Skills { get; init; }
}

/// <summary>
/// Individual skill entry in the RFC index.
/// </summary>
public sealed record RfcSkillEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RfcResourceEntry>? Resources { get; init; }
}

/// <summary>
/// Resource file entry for skills with sub-resources.
/// </summary>
public sealed record RfcResourceEntry
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("digest")]
    public required string Digest { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("unixMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UnixMode { get; init; }
}
