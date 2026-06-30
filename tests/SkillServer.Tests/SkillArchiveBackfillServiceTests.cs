// -----------------------------------------------------------------------
// <copyright file="SkillArchiveBackfillServiceTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.IO.Compression;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SkillServer.Data;
using SkillServer.Models;
using SkillServer.Services;
using Xunit;

namespace SkillServer.Tests;

public sealed class SkillArchiveBackfillServiceTests : IDisposable
{
    private readonly string _tempDir;

    public SkillArchiveBackfillServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillserver-backfill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task BackfillAsync_ConvertsLegacyResourcefulSkillToArchiveArtifact()
    {
        var ct = TestContext.Current.CancellationToken;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SkillServer:DataPath"] = _tempDir
            })
            .Build();

        var initializer = new DatabaseInitializer(configuration, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync(ct);

        var repository = new SkillRepository(initializer);
        var blobStorage = new BlobStorage(configuration, NullLogger<BlobStorage>.Instance);
        var backfillService = new SkillArchiveBackfillService(
            repository,
            blobStorage,
            NullLogger<SkillArchiveBackfillService>.Instance);

        const string skillName = "legacy-resourceful";
        var skillMdBytes = Encoding.UTF8.GetBytes($"---\nname: {skillName}\ndescription: Legacy resourceful skill\n---\n# Legacy");
        var resourceBytes = Encoding.UTF8.GetBytes("# Guide");

        var (skillDigest, skillSizeBytes) = await blobStorage.StoreAsync(skillMdBytes, ct);
        var (resourceDigest, resourceSizeBytes) = await blobStorage.StoreAsync(resourceBytes, ct);

        var skillId = await repository.CreateSkillAsync(skillName, ct);
        var versionId = await repository.CreateVersionAsync(
            skillId,
            "1.0.0",
            "Legacy resourceful skill",
            null,
            SkillTypes.SkillMd,
            skillDigest,
            skillSizeBytes,
            ct);
        await repository.AddFileAsync(versionId, "references/guide.md", resourceDigest, resourceSizeBytes, ct);

        await backfillService.BackfillAsync(ct);

        var version = await repository.GetVersionAsync(skillId, "1.0.0", ct);
        Assert.NotNull(version);
        Assert.Equal(SkillTypes.Archive, version.SkillType);
        Assert.Equal(skillDigest, version.Sha256);
        Assert.NotEqual(version.Sha256, version.ArtifactSha256);
        Assert.True(version.ArtifactSizeBytes > version.SizeBytes);

        await using var archiveBlob = blobStorage.GetBlob(version.ArtifactSha256);
        Assert.NotNull(archiveBlob);

        using var archive = new ZipArchive(archiveBlob!, ZipArchiveMode.Read);
        Assert.Equal(["SKILL.md", "references/guide.md"], archive.Entries.Select(e => e.FullName).ToArray());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
