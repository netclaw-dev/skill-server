// -----------------------------------------------------------------------
// <copyright file="SkillArchiveBackfillService.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using SkillServer.Data;
using SkillServer.Models;

namespace SkillServer.Services;

public sealed class SkillArchiveBackfillService
{
    private readonly SkillRepository _repository;
    private readonly BlobStorage _blobStorage;
    private readonly ILogger<SkillArchiveBackfillService> _logger;

    public SkillArchiveBackfillService(
        SkillRepository repository,
        BlobStorage blobStorage,
        ILogger<SkillArchiveBackfillService> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task BackfillAsync(CancellationToken ct = default)
    {
        var versions = await _repository.GetResourcefulVersionsWithoutArchiveAsync(ct);
        if (versions.Count == 0)
            return;

        _logger.LogInformation("Backfilling archive artifacts for {Count} resourceful skill version(s)", versions.Count);

        foreach (var version in versions)
        {
            await BackfillVersionAsync(version, ct);
        }
    }

    private async Task BackfillVersionAsync(SkillVersion version, CancellationToken ct)
    {
        var skillMdBytes = await ReadBlobBytesAsync(version.Sha256, ct);
        if (skillMdBytes is null)
        {
            _logger.LogWarning("Skipping archive backfill for skill version id {VersionId}: SKILL.md blob {Digest} is missing", version.Id, version.Sha256);
            return;
        }

        var files = await _repository.GetFilesAsync(version.Id, ct);
        var resources = new List<(ResourcePath Path, byte[] Content)>(files.Count);

        foreach (var file in files)
        {
            if (!ResourcePath.TryCreate(file.RelativePath, out var resourcePath))
            {
                _logger.LogWarning("Skipping archive backfill for skill version id {VersionId}: invalid resource path {Path}", version.Id, file.RelativePath);
                return;
            }

            var content = await ReadBlobBytesAsync(file.Sha256, ct);
            if (content is null)
            {
                _logger.LogWarning("Skipping archive backfill for skill version id {VersionId}: resource blob {Digest} is missing", version.Id, file.Sha256);
                return;
            }

            resources.Add((resourcePath.Value, content));
        }

        var archiveBytes = SkillArchiveBuilder.BuildZip(skillMdBytes, resources);
        var (archiveDigest, archiveSizeBytes) = await _blobStorage.StoreAsync(archiveBytes, ct);
        var parsedArchiveDigest = Sha256Digest.Create(archiveDigest);

        await _repository.UpdateVersionArtifactAsync(
            version.Id, SkillTypes.Archive, parsedArchiveDigest.Value, archiveSizeBytes, ct);
    }

    private async Task<byte[]?> ReadBlobBytesAsync(string digest, CancellationToken ct)
    {
        await using var stream = _blobStorage.GetBlob(digest);
        if (stream is null)
            return null;

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
