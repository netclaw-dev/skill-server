// -----------------------------------------------------------------------
// <copyright file="DatabaseInitializer.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace SkillServer.Data;

/// <summary>
/// Initializes the SQLite database schema via numbered migrations.
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
        await SchemaMigrator.MigrateAsync(_connectionString, _logger, ct);
    }
}
