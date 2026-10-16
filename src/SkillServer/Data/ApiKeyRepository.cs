// -----------------------------------------------------------------------
// <copyright file="ApiKeyRepository.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Dapper;
using Microsoft.Data.Sqlite;
using SkillServer.Models;

namespace SkillServer.Data;

public sealed class ApiKeyRepository
{
    private readonly string _connectionString;

    public ApiKeyRepository(DatabaseInitializer initializer)
    {
        _connectionString = initializer.ConnectionString;
    }

    public async Task<long> CreateKeyAsync(string label, string keyHash, DateTimeOffset? expiresAt,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var expiresAtStr = expiresAt?.ToString("O");
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO api_keys (label, key_hash, created_at, expires_at)
            VALUES (@label, @keyHash, @now, @expiresAtStr);
            SELECT last_insert_rowid();
            """,
            new { label, keyHash, now, expiresAtStr },
            null, null, null);
    }

    public async Task<ApiKey?> GetKeyByHashAsync(string keyHash, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<ApiKey>(
            """
            SELECT id AS Id, label AS Label, key_hash AS KeyHash,
                   created_at AS CreatedAt, expires_at AS ExpiresAt
            FROM api_keys WHERE key_hash = @keyHash
            """,
            new { keyHash },
            null, null, null);
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllKeysAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var keys = await connection.QueryAsync<ApiKey>(
            """
            SELECT id AS Id, label AS Label, key_hash AS KeyHash,
                   created_at AS CreatedAt, expires_at AS ExpiresAt
            FROM api_keys ORDER BY created_at DESC
            """,
            null, null, null, null);
        return keys.ToList();
    }

    public async Task<bool> DeleteKeyIfNotLastAsync(long id, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var deleted = await connection.ExecuteAsync(
            """
            DELETE FROM api_keys
            WHERE id = @id AND (SELECT COUNT(*) FROM api_keys) > 1
            """,
            new { id },
            null, null, null);
        return deleted > 0;
    }

    public async Task<bool> AnyKeysExistAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT EXISTS(SELECT 1 FROM api_keys LIMIT 1)",
            null, null, null, null);
        return exists == 1;
    }
}
