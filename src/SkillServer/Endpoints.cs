// -----------------------------------------------------------------------
// <copyright file="Endpoints.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.Mvc;
using SkillServer.Data;
using SkillServer.Models;
using SkillServer.Services;

namespace SkillServer;

public static class Endpoints
{
    public static WebApplication MapSkillServerEndpoints(this WebApplication app)
    {
        app.MapDiscoveryEndpoints();
        app.MapSkillEndpoints();
        app.MapBlobEndpoints();
        app.MapHealthEndpoints();
        return app;
    }

    private static void MapDiscoveryEndpoints(this WebApplication app)
    {
        app.MapGet("/.well-known/agent-skills/index.json", async (
            IndexGenerator indexGenerator,
            CancellationToken ct) =>
        {
            var index = await indexGenerator.GenerateRfcIndexAsync(ct);
            return Results.Json(index, SkillServerJsonContext.Default.RfcSkillIndex);
        });
    }

    private static void MapSkillEndpoints(this WebApplication app)
    {
        var skills = app.MapGroup("/skills");

        skills.MapGet("/", ListSkills);
        skills.MapGet("/{name}", GetSkill);
        skills.MapGet("/{name}/{version}", GetVersion);
        skills.MapGet("/{name}/{version}/SKILL.md", DownloadSkillMd);
        skills.MapGet("/{name}/{version}/{*path}", DownloadResource);
        skills.MapPost("/", UploadSkill).DisableAntiforgery();
        skills.MapDelete("/{name}/{version}", DeleteVersion);
    }

    private static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));
    }

    private static async Task<IResult> ListSkills(
        SkillRepository repository,
        int? skip,
        int? take,
        CancellationToken ct)
    {
        var latestVersions = await repository.GetAllLatestVersionsWithMetadataAsync(skip, take, ct);

        var summaries = latestVersions.Select(v => new SkillSummary
        {
            Name = v.SkillName,
            Description = v.Description,
            LatestVersion = v.Version,
            Category = v.Category,
            VersionCount = v.VersionCount,
            CreatedAt = v.SkillCreatedAt,
            UpdatedAt = v.SkillUpdatedAt
        }).ToList();

        return Results.Json(summaries, SkillServerJsonContext.Default.IReadOnlyListSkillSummary);
    }

    private static async Task<IResult> GetSkill(
        string name,
        SkillRepository repository,
        CancellationToken ct)
    {
        var skill = await repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var versions = await repository.GetAllVersionsWithFileCountAsync(skill.Id, ct);
        var summaries = versions.Select(v => new SkillVersionSummary
        {
            Name = skill.Name,
            Version = v.Version,
            Description = v.Description,
            Category = v.Category,
            Sha256 = v.Sha256,
            SizeBytes = v.SizeBytes,
            PublishedAt = v.PublishedAt,
            IsLatest = v.IsLatest,
            FileCount = v.FileCount
        }).ToList();

        return Results.Json(summaries, SkillServerJsonContext.Default.IReadOnlyListSkillVersionSummary);
    }

    private static async Task<IResult> GetVersion(
        string name,
        string version,
        SkillRepository repository,
        CancellationToken ct)
    {
        var skill = await repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var skillVersion = await repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found for skill '{name}'." });

        var files = await repository.GetFilesAsync(skillVersion.Id, ct);

        var summary = new SkillVersionSummary
        {
            Name = skill.Name,
            Version = skillVersion.Version,
            Description = skillVersion.Description,
            Category = skillVersion.Category,
            Sha256 = skillVersion.Sha256,
            SizeBytes = skillVersion.SizeBytes,
            PublishedAt = skillVersion.PublishedAt,
            IsLatest = skillVersion.IsLatest,
            FileCount = files.Count
        };

        return Results.Json(summary, SkillServerJsonContext.Default.SkillVersionSummary);
    }

    private static async Task<IResult> DownloadSkillMd(
        string name,
        string version,
        SkillRepository repository,
        BlobStorage blobStorage,
        CancellationToken ct)
    {
        var skill = await repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return Results.NotFound();

        var skillVersion = await repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return Results.NotFound();

        var stream = blobStorage.GetBlob(skillVersion.Sha256);
        if (stream is null)
            return Results.NotFound();

        return Results.File(stream, "text/markdown", "SKILL.md");
    }

    private static async Task<IResult> DownloadResource(
        string name,
        string version,
        string path,
        SkillRepository repository,
        BlobStorage blobStorage,
        CancellationToken ct)
    {
        var skill = await repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return Results.NotFound();

        var skillVersion = await repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return Results.NotFound();

        var files = await repository.GetFilesAsync(skillVersion.Id, ct);
        var file = files.FirstOrDefault(f => f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (file is null)
            return Results.NotFound();

        var stream = blobStorage.GetBlob(file.Sha256);
        if (stream is null)
            return Results.NotFound();

        return Results.File(stream, GetContentType(path));
    }

    private static async Task<IResult> UploadSkill(
        [FromForm] IFormFile file,
        [FromForm] string name,
        [FromForm] string version,
        [FromForm] string? category,
        SkillUploadService uploadService,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!SkillName.TryCreate(name, out var skillName))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_name",
                Message = "Invalid skill name. Must be 1-64 lowercase alphanumeric characters and hyphens."
            });
        }

        if (!SkillVersionString.TryCreate(version, out var skillVersion))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_version",
                Message = "Invalid version string."
            });
        }

        if (file.FileName != "SKILL.md" && !file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_file",
                Message = "File must be SKILL.md or a markdown file."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await uploadService.UploadSkillMdAsync(skillName.Value, skillVersion.Value, stream, category, ct);

        if (!result.Success)
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "upload_failed",
                Message = result.Error ?? "Upload failed."
            });
        }

        var baseUrl = configuration["SkillServer:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";

        return Results.Created(
            $"/skills/{result.Name}/{result.Version}",
            new SkillUploadResponse
            {
                Name = result.Name!.Value.Value,
                Version = result.Version!.Value.Value,
                Sha256 = result.Digest!.Value.Value,
                Url = $"{baseUrl}/skills/{result.Name}/{result.Version}/SKILL.md"
            });
    }

    private static async Task<IResult> DeleteVersion(
        string name,
        string version,
        SkillRepository repository,
        CancellationToken ct)
    {
        var skill = await repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var deleted = await repository.DeleteVersionAsync(skill.Id, version, ct);
        if (!deleted)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found." });

        return Results.NoContent();
    }

    private static void MapBlobEndpoints(this WebApplication app)
    {
        app.MapGet("/blobs/sha256/{digest}", (
            string digest,
            BlobStorage blobStorage) =>
        {
            var stream = blobStorage.GetBlob(digest);
            if (stream is null)
                return Results.NotFound();

            return Results.File(stream, "application/octet-stream");
        });
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".md" => "text/markdown",
        ".json" => "application/json",
        ".yaml" or ".yml" => "application/x-yaml",
        ".py" => "text/x-python",
        ".sh" => "application/x-sh",
        ".js" => "application/javascript",
        ".ts" => "application/typescript",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
