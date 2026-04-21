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
    public required SkillType SkillType { get; init; }
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
/// Type of skill artifact.
/// </summary>
public enum SkillType
{
    /// <summary>Single SKILL.md file.</summary>
    SkillMd,

    /// <summary>Archive containing SKILL.md and resources.</summary>
    Archive
}
