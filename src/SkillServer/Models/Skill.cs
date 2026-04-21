namespace SkillServer.Models;

/// <summary>
/// Represents a skill in the registry.
/// </summary>
public sealed record Skill
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Represents a specific version of a skill.
/// </summary>
public sealed record SkillVersion
{
    public required long Id { get; init; }
    public required long SkillId { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public string? Category { get; init; }
    public required string SkillType { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required bool IsLatest { get; init; }
}

/// <summary>
/// Represents a file within a skill version (for directory-based skills).
/// </summary>
public sealed record SkillFile
{
    public required long Id { get; init; }
    public required long SkillVersionId { get; init; }
    public required string RelativePath { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
}

/// <summary>
/// Skill version with file count for batch queries.
/// </summary>
public sealed record SkillVersionWithFileCount
{
    public required long Id { get; init; }
    public required long SkillId { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public string? Category { get; init; }
    public required string SkillType { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required bool IsLatest { get; init; }
    public required int FileCount { get; init; }
}

/// <summary>
/// Skill version with denormalized skill metadata for batch queries.
/// </summary>
public sealed record SkillVersionWithMetadata
{
    public required long Id { get; init; }
    public required long SkillId { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public string? Category { get; init; }
    public required string SkillType { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required bool IsLatest { get; init; }
    public required string SkillName { get; init; }
    public required DateTimeOffset SkillCreatedAt { get; init; }
    public required DateTimeOffset SkillUpdatedAt { get; init; }
    public required int VersionCount { get; init; }
}

/// <summary>
/// Skill type constants.
/// </summary>
public static class SkillTypes
{
    public const string SkillMd = "skill-md";
    public const string Archive = "archive";
}
