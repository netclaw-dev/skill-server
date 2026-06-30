// -----------------------------------------------------------------------
// <copyright file="SubAgentRepository.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Dapper;
using Microsoft.Data.Sqlite;
using SkillServer.Models;

namespace SkillServer.Data;

public sealed class SubAgentRepository
{
    private readonly string _connectionString;

    public SubAgentRepository(DatabaseInitializer initializer)
    {
        DapperConfiguration.Initialize();
        _connectionString = initializer.ConnectionString;
    }

    public async Task<SubAgent?> GetSubAgentByNameAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<SubAgent>(
            """
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM subagents WHERE name = @name COLLATE NOCASE
            """,
            new { name });
    }

    public async Task<long> CreateSubAgentAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToString("O");
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO subagents (name, created_at, updated_at)
            VALUES (@name, @now, @now);
            SELECT last_insert_rowid();
            """,
            new { name, now });
    }

    public async Task UpdateSubAgentTimestampAsync(long subAgentId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToString("O");
        await connection.ExecuteAsync(
            "UPDATE subagents SET updated_at = @now WHERE id = @subAgentId",
            new { subAgentId, now });
    }

    public async Task<IReadOnlyList<SubAgentVersionWithMetadata>> GetAllLatestVersionsWithMetadataAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var versions = await connection.QueryAsync<SubAgentVersionWithMetadata>(
            """
            SELECT sv.id AS Id, sv.subagent_id AS SubAgentId, sv.version AS Version,
                   sv.description AS Description, sv.model_role AS ModelRole,
                   sv.timeout_seconds AS TimeoutSeconds,
                   sv.prefill_timeout_seconds AS PrefillTimeoutSeconds,
                   sv.visibility AS Visibility,
                   sv.emit_structured_findings AS EmitStructuredFindings,
                   sv.sha256 AS Sha256, sv.size_bytes AS SizeBytes,
                   sv.published_at AS PublishedAt, sv.is_latest AS IsLatest,
                   s.name AS SubAgentName, s.created_at AS SubAgentCreatedAt,
                   s.updated_at AS SubAgentUpdatedAt,
                   (SELECT COUNT(*) FROM subagent_versions sv2 WHERE sv2.subagent_id = s.id) AS VersionCount
            FROM subagent_versions sv
            JOIN subagents s ON s.id = sv.subagent_id
            WHERE sv.is_latest = 1
            ORDER BY s.name
            """);
        return versions.ToList();
    }

    public async Task<IReadOnlyList<SubAgentVersion>> GetAllVersionsAsync(long subAgentId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var versions = await connection.QueryAsync<SubAgentVersion>(
            """
            SELECT id AS Id, subagent_id AS SubAgentId, version AS Version,
                   description AS Description, model_role AS ModelRole,
                   timeout_seconds AS TimeoutSeconds,
                   prefill_timeout_seconds AS PrefillTimeoutSeconds,
                   visibility AS Visibility,
                   emit_structured_findings AS EmitStructuredFindings,
                   sha256 AS Sha256, size_bytes AS SizeBytes,
                   published_at AS PublishedAt, is_latest AS IsLatest
            FROM subagent_versions
            WHERE subagent_id = @subAgentId
            ORDER BY published_at DESC
            """,
            new { subAgentId });
        return versions.ToList();
    }

    public async Task<SubAgentVersion?> GetVersionAsync(long subAgentId, string version, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<SubAgentVersion>(
            """
            SELECT id AS Id, subagent_id AS SubAgentId, version AS Version,
                   description AS Description, model_role AS ModelRole,
                   timeout_seconds AS TimeoutSeconds,
                   prefill_timeout_seconds AS PrefillTimeoutSeconds,
                   visibility AS Visibility,
                   emit_structured_findings AS EmitStructuredFindings,
                   sha256 AS Sha256, size_bytes AS SizeBytes,
                   published_at AS PublishedAt, is_latest AS IsLatest
            FROM subagent_versions
            WHERE subagent_id = @subAgentId AND version = @version
            """,
            new { subAgentId, version });
    }

    public async Task<long> CreateVersionAsync(
        long subAgentId,
        string version,
        string description,
        string modelRole,
        int timeoutSeconds,
        int? prefillTimeoutSeconds,
        string visibility,
        bool emitStructuredFindings,
        string sha256,
        long sizeBytes,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            await connection.ExecuteAsync(
                "UPDATE subagent_versions SET is_latest = 0 WHERE subagent_id = @subAgentId",
                new { subAgentId },
                transaction);

            var now = DateTimeOffset.UtcNow.ToString("O");
            var versionId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO subagent_versions (
                    subagent_id, version, description, model_role, timeout_seconds,
                    prefill_timeout_seconds, visibility, emit_structured_findings,
                    sha256, size_bytes, published_at, is_latest)
                VALUES (
                    @subAgentId, @version, @description, @modelRole, @timeoutSeconds,
                    @prefillTimeoutSeconds, @visibility, @emitStructuredFindings,
                    @sha256, @sizeBytes, @now, 1);
                SELECT last_insert_rowid();
                """,
                new
                {
                    subAgentId,
                    version,
                    description,
                    modelRole,
                    timeoutSeconds,
                    prefillTimeoutSeconds,
                    visibility,
                    emitStructuredFindings,
                    sha256,
                    sizeBytes,
                    now
                },
                transaction);

            await transaction.CommitAsync(ct);
            return versionId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> DeleteVersionAsync(long subAgentId, string version, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var deleted = await connection.ExecuteAsync(
            "DELETE FROM subagent_versions WHERE subagent_id = @subAgentId AND version = @version",
            new { subAgentId, version });
        return deleted > 0;
    }
}
