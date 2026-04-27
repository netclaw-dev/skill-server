// -----------------------------------------------------------------------
// <copyright file="SchemaMigrator.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace SkillServer.Data;

public static class SchemaMigrator
{
    public static async Task MigrateAsync(string connectionString, ILogger logger,
        CancellationToken ct = default)
    {
        logger.LogInformation("Starting database schema migration...");

        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS schema_version (
                    version INT PRIMARY KEY,
                    name TEXT NOT NULL,
                    applied_at TEXT NOT NULL
                );
                """;
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var appliedMigrations = new HashSet<int>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT version FROM schema_version";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                appliedMigrations.Add(reader.GetInt32(0));
            }
        }

        var migrationsDir = Path.Combine(AppContext.BaseDirectory, "migrations");
        if (!Directory.Exists(migrationsDir))
        {
            logger.LogWarning("Migrations directory not found: {MigrationsDir}", migrationsDir);
            return;
        }

        var migrationFiles = Directory.GetFiles(migrationsDir, "*.sql")
            .Select(f => new { Path = f, Name = Path.GetFileName(f) })
            .Select(f => new { f.Path, f.Name, Version = ParseVersion(f.Name) })
            .Where(f => f.Version != null)
            .OrderBy(f => f.Version)
            .ToList();

        foreach (var migration in migrationFiles)
        {
            var version = migration.Version!.Value;
            if (appliedMigrations.Contains(version))
            {
                logger.LogInformation("Migration {Name} already applied", migration.Name);
                continue;
            }

            logger.LogInformation("Applying migration: {Name}", migration.Name);
            var sql = await File.ReadAllTextAsync(migration.Path, ct);

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO schema_version (version, name, applied_at) VALUES (@version, @name, @appliedAt)";
                cmd.Parameters.AddWithValue("@version", version);
                cmd.Parameters.AddWithValue("@name", migration.Name);
                cmd.Parameters.AddWithValue("@appliedAt", DateTimeOffset.UtcNow.ToString("o"));
                await cmd.ExecuteNonQueryAsync(ct);
            }

            logger.LogInformation("Migration {Name} applied successfully", migration.Name);
        }

        logger.LogInformation("Database schema migration completed successfully");
    }

    internal static int? ParseVersion(string fileName)
    {
        var parts = fileName.Split('_', 2);
        if (parts.Length < 2) return null;
        if (int.TryParse(parts[0], out var version)) return version;
        return null;
    }
}
