using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SkillServer.Models;

namespace SkillServer.Data;

/// <summary>
/// Initializes Dapper type handlers for SQLite.
/// </summary>
public static class DapperConfiguration
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        _initialized = true;
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
            parameter.Value = value.ToString("O");

        public override DateTimeOffset Parse(object value) =>
            DateTimeOffset.Parse(value.ToString()!);
    }
}

/// <summary>
/// Repository for skill metadata operations using Dapper.
/// </summary>
public sealed class SkillRepository
{
    private readonly string _connectionString;

    public SkillRepository(DatabaseInitializer initializer)
    {
        DapperConfiguration.Initialize();
        _connectionString = initializer.ConnectionString;
    }

    public async Task<Skill?> GetSkillByNameAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Skill>(
            """
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM skills WHERE name = @name COLLATE NOCASE
            """,
            new { name });
    }

    public async Task<IReadOnlyList<Skill>> GetAllSkillsAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var skills = await connection.QueryAsync<Skill>(
            """
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM skills ORDER BY name
            """);
        return skills.ToList();
    }

    public async Task<long> CreateSkillAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToString("O");
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO skills (name, created_at, updated_at)
            VALUES (@name, @now, @now);
            SELECT last_insert_rowid();
            """,
            new { name, now });
    }

    public async Task UpdateSkillTimestampAsync(long skillId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToString("O");
        await connection.ExecuteAsync(
            "UPDATE skills SET updated_at = @now WHERE id = @skillId",
            new { skillId, now });
    }

    public async Task<SkillVersion?> GetLatestVersionAsync(long skillId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<SkillVersion>(
            """
            SELECT id AS Id, skill_id AS SkillId, version AS Version, description AS Description,
                   category AS Category, skill_type AS SkillType, sha256 AS Sha256,
                   size_bytes AS SizeBytes, published_at AS PublishedAt, is_latest AS IsLatest
            FROM skill_versions
            WHERE skill_id = @skillId AND is_latest = 1
            """,
            new { skillId });
    }

    public async Task<SkillVersion?> GetVersionAsync(long skillId, string version, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<SkillVersion>(
            """
            SELECT id AS Id, skill_id AS SkillId, version AS Version, description AS Description,
                   category AS Category, skill_type AS SkillType, sha256 AS Sha256,
                   size_bytes AS SizeBytes, published_at AS PublishedAt, is_latest AS IsLatest
            FROM skill_versions
            WHERE skill_id = @skillId AND version = @version
            """,
            new { skillId, version });
    }

    public async Task<IReadOnlyList<SkillVersion>> GetAllVersionsAsync(long skillId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var versions = await connection.QueryAsync<SkillVersion>(
            """
            SELECT id AS Id, skill_id AS SkillId, version AS Version, description AS Description,
                   category AS Category, skill_type AS SkillType, sha256 AS Sha256,
                   size_bytes AS SizeBytes, published_at AS PublishedAt, is_latest AS IsLatest
            FROM skill_versions
            WHERE skill_id = @skillId
            ORDER BY published_at DESC
            """,
            new { skillId });
        return versions.ToList();
    }

    public async Task<IReadOnlyList<SkillVersionWithFileCount>> GetAllVersionsWithFileCountAsync(long skillId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var versions = await connection.QueryAsync<SkillVersionWithFileCount>(
            """
            SELECT sv.id AS Id, sv.skill_id AS SkillId, sv.version AS Version, sv.description AS Description,
                   sv.category AS Category, sv.skill_type AS SkillType, sv.sha256 AS Sha256,
                   sv.size_bytes AS SizeBytes, sv.published_at AS PublishedAt, sv.is_latest AS IsLatest,
                   (SELECT COUNT(*) FROM skill_files sf WHERE sf.skill_version_id = sv.id) AS FileCount
            FROM skill_versions sv
            WHERE sv.skill_id = @skillId
            ORDER BY sv.published_at DESC
            """,
            new { skillId });
        return versions.ToList();
    }

    public async Task<IReadOnlyList<SkillVersionWithMetadata>> GetAllLatestVersionsWithMetadataAsync(
        int? skip = null,
        int? take = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var sql = """
            SELECT sv.id AS Id, sv.skill_id AS SkillId, sv.version AS Version, sv.description AS Description,
                   sv.category AS Category, sv.skill_type AS SkillType, sv.sha256 AS Sha256,
                   sv.size_bytes AS SizeBytes, sv.published_at AS PublishedAt, sv.is_latest AS IsLatest,
                   s.name AS SkillName, s.created_at AS SkillCreatedAt, s.updated_at AS SkillUpdatedAt,
                   (SELECT COUNT(*) FROM skill_versions sv2 WHERE sv2.skill_id = s.id) AS VersionCount
            FROM skill_versions sv
            JOIN skills s ON s.id = sv.skill_id
            WHERE sv.is_latest = 1
            ORDER BY s.name
            """;

        if (take.HasValue)
            sql += $" LIMIT {take.Value}";
        if (skip.HasValue)
            sql += $" OFFSET {skip.Value}";

        var versions = await connection.QueryAsync<SkillVersionWithMetadata>(sql);
        return versions.ToList();
    }

    public async Task<int> GetVersionCountAsync(long skillId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM skill_versions WHERE skill_id = @skillId",
            new { skillId });
    }

    public async Task<long> CreateVersionAsync(
        long skillId,
        string version,
        string description,
        string? category,
        string skillType,
        string sha256,
        long sizeBytes,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // Clear previous latest flag
            await connection.ExecuteAsync(
                "UPDATE skill_versions SET is_latest = 0 WHERE skill_id = @skillId",
                new { skillId },
                transaction);

            var now = DateTimeOffset.UtcNow.ToString("O");
            var typeString = skillType;

            var versionId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO skill_versions (skill_id, version, description, category, skill_type, sha256, size_bytes, published_at, is_latest)
                VALUES (@skillId, @version, @description, @category, @typeString, @sha256, @sizeBytes, @now, 1);
                SELECT last_insert_rowid();
                """,
                new { skillId, version, description, category, typeString, sha256, sizeBytes, now },
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

    public async Task<IReadOnlyList<SkillFile>> GetFilesAsync(long versionId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var files = await connection.QueryAsync<SkillFile>(
            """
            SELECT id AS Id, skill_version_id AS SkillVersionId, relative_path AS RelativePath,
                   sha256 AS Sha256, size_bytes AS SizeBytes
            FROM skill_files
            WHERE skill_version_id = @versionId
            ORDER BY relative_path
            """,
            new { versionId });
        return files.ToList();
    }

    public async Task AddFileAsync(long versionId, string relativePath, string sha256, long sizeBytes, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO skill_files (skill_version_id, relative_path, sha256, size_bytes)
            VALUES (@versionId, @relativePath, @sha256, @sizeBytes)
            """,
            new { versionId, relativePath, sha256, sizeBytes });
    }

    public async Task<bool> DeleteVersionAsync(long skillId, string version, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var deleted = await connection.ExecuteAsync(
            "DELETE FROM skill_versions WHERE skill_id = @skillId AND version = @version",
            new { skillId, version });
        return deleted > 0;
    }
}
