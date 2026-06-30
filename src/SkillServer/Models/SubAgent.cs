// -----------------------------------------------------------------------
// <copyright file="SubAgent.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace SkillServer.Models;

public sealed record SubAgent
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SubAgentVersion
{
    public required long Id { get; init; }
    public required long SubAgentId { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required string ModelRole { get; init; }
    public required int TimeoutSeconds { get; init; }
    public int? PrefillTimeoutSeconds { get; init; }
    public required string Visibility { get; init; }
    public required bool EmitStructuredFindings { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required bool IsLatest { get; init; }
}

public sealed record SubAgentVersionWithMetadata
{
    public required long Id { get; init; }
    public required long SubAgentId { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required string ModelRole { get; init; }
    public required int TimeoutSeconds { get; init; }
    public int? PrefillTimeoutSeconds { get; init; }
    public required string Visibility { get; init; }
    public required bool EmitStructuredFindings { get; init; }
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required bool IsLatest { get; init; }
    public required string SubAgentName { get; init; }
    public required DateTimeOffset SubAgentCreatedAt { get; init; }
    public required DateTimeOffset SubAgentUpdatedAt { get; init; }
    public required int VersionCount { get; init; }
}

public static class SubAgentTypes
{
    public const string AgentMd = "agent-md";
}

public static class SubAgentModelRoles
{
    public const string Compaction = "Compaction";
    public const string Main = "Main";
}

public static class SubAgentVisibility
{
    public const string UserFacing = "user-facing";
    public const string Internal = "internal";
}
