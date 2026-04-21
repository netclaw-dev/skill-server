using Dapper;
using Microsoft.Data.Sqlite;

namespace SkillServer.Data;

/// <summary>
/// Initializes the SQLite database schema.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        var dataPath = configuration["SkillServer:DataPath"] ?? "./data";
        Directory.CreateDirectory(dataPath);
        _connectionString = $"Data Source={Path.Combine(dataPath, "skills.db")}";
        _logger = logger;
    }

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        _logger.LogInformation("Initializing database schema...");

        await connection.ExecuteAsync(Schema);

        _logger.LogInformation("Database schema initialized");
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS skills (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE COLLATE NOCASE,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS skill_versions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
            version TEXT NOT NULL,
            description TEXT NOT NULL,
            category TEXT,
            skill_type TEXT NOT NULL CHECK(skill_type IN ('skill-md', 'archive')),
            sha256 TEXT NOT NULL,
            size_bytes INTEGER NOT NULL,
            published_at TEXT NOT NULL,
            is_latest INTEGER NOT NULL DEFAULT 0,
            UNIQUE(skill_id, version)
        );

        CREATE TABLE IF NOT EXISTS skill_files (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            skill_version_id INTEGER NOT NULL REFERENCES skill_versions(id) ON DELETE CASCADE,
            relative_path TEXT NOT NULL,
            sha256 TEXT NOT NULL,
            size_bytes INTEGER NOT NULL,
            UNIQUE(skill_version_id, relative_path)
        );

        CREATE INDEX IF NOT EXISTS idx_skill_versions_latest
            ON skill_versions(skill_id) WHERE is_latest = 1;

        CREATE INDEX IF NOT EXISTS idx_skill_versions_sha256
            ON skill_versions(sha256);

        CREATE INDEX IF NOT EXISTS idx_skill_files_sha256
            ON skill_files(sha256);
        """;
}
