using Microsoft.AspNetCore.Mvc;
using SkillServer.Data;
using SkillServer.Models;
using SkillServer.Services;

namespace SkillServer.Controllers;

/// <summary>
/// CRUD operations for skills.
/// </summary>
[ApiController]
[Route("skills")]
public sealed class SkillsController : ControllerBase
{
    private readonly SkillRepository _repository;
    private readonly BlobStorage _blobStorage;
    private readonly SkillUploadService _uploadService;
    private readonly IConfiguration _configuration;

    public SkillsController(
        SkillRepository repository,
        BlobStorage blobStorage,
        SkillUploadService uploadService,
        IConfiguration configuration)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _uploadService = uploadService;
        _configuration = configuration;
    }

    /// <summary>
    /// List all skills.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListSkills(CancellationToken ct)
    {
        var skills = await _repository.GetAllSkillsAsync(ct);
        var summaries = new List<SkillSummary>();

        foreach (var skill in skills)
        {
            var latestVersion = await _repository.GetLatestVersionAsync(skill.Id, ct);
            if (latestVersion is null) continue;

            var allVersions = await _repository.GetAllVersionsAsync(skill.Id, ct);

            summaries.Add(new SkillSummary
            {
                Name = skill.Name,
                Description = latestVersion.Description,
                LatestVersion = latestVersion.Version,
                Category = latestVersion.Category,
                VersionCount = allVersions.Count,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt
            });
        }

        return new JsonResult(summaries, SkillServerJsonContext.Default.IReadOnlyListSkillSummary);
    }

    /// <summary>
    /// Get skill info (all versions).
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetSkill(string name, CancellationToken ct)
    {
        var skill = await _repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var versions = await _repository.GetAllVersionsAsync(skill.Id, ct);
        var summaries = new List<SkillVersionSummary>();

        foreach (var version in versions)
        {
            var files = await _repository.GetFilesAsync(version.Id, ct);
            summaries.Add(new SkillVersionSummary
            {
                Name = skill.Name,
                Version = version.Version,
                Description = version.Description,
                Category = version.Category,
                Sha256 = version.Sha256,
                SizeBytes = version.SizeBytes,
                PublishedAt = version.PublishedAt,
                IsLatest = version.IsLatest,
                FileCount = files.Count
            });
        }

        return new JsonResult(summaries, SkillServerJsonContext.Default.IReadOnlyListSkillVersionSummary);
    }

    /// <summary>
    /// Get specific version metadata.
    /// </summary>
    [HttpGet("{name}/{version}")]
    public async Task<IActionResult> GetVersion(string name, string version, CancellationToken ct)
    {
        var skill = await _repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var skillVersion = await _repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found for skill '{name}'." });

        var files = await _repository.GetFilesAsync(skillVersion.Id, ct);

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

        return new JsonResult(summary, SkillServerJsonContext.Default.SkillVersionSummary);
    }

    /// <summary>
    /// Download SKILL.md for a version.
    /// </summary>
    [HttpGet("{name}/{version}/SKILL.md")]
    public async Task<IActionResult> DownloadSkillMd(string name, string version, CancellationToken ct)
    {
        var skill = await _repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return NotFound();

        var skillVersion = await _repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return NotFound();

        var stream = _blobStorage.GetBlob(skillVersion.Sha256);
        if (stream is null)
            return NotFound();

        return File(stream, "text/markdown", "SKILL.md");
    }

    /// <summary>
    /// Download a resource file.
    /// </summary>
    [HttpGet("{name}/{version}/{**path}")]
    public async Task<IActionResult> DownloadResource(string name, string version, string path, CancellationToken ct)
    {
        var skill = await _repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return NotFound();

        var skillVersion = await _repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return NotFound();

        var files = await _repository.GetFilesAsync(skillVersion.Id, ct);
        var file = files.FirstOrDefault(f => f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (file is null)
            return NotFound();

        var stream = _blobStorage.GetBlob(file.Sha256);
        if (stream is null)
            return NotFound();

        var contentType = GetContentType(path);
        return File(stream, contentType);
    }

    /// <summary>
    /// Upload a new skill version.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSkill(
        [FromForm] string name,
        [FromForm] string version,
        [FromForm] string? category,
        IFormFile file,
        CancellationToken ct)
    {
        if (!SkillName.TryCreate(name, out var skillName))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "invalid_name",
                Message = "Invalid skill name. Must be 1-64 lowercase alphanumeric characters and hyphens."
            });
        }

        if (!SkillVersionString.TryCreate(version, out var skillVersion))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "invalid_version",
                Message = "Invalid version string."
            });
        }

        if (file.FileName != "SKILL.md" && !file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "invalid_file",
                Message = "File must be SKILL.md or a markdown file."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _uploadService.UploadSkillMdAsync(skillName.Value, skillVersion.Value, stream, category, ct);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "upload_failed",
                Message = result.Error ?? "Upload failed."
            });
        }

        var baseUrl = _configuration["SkillServer:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";

        return Created(
            $"/skills/{result.Name}/{result.Version}",
            new SkillUploadResponse
            {
                Name = result.Name!.Value.Value,
                Version = result.Version!.Value.Value,
                Sha256 = result.Digest!.Value.Value,
                Url = $"{baseUrl}/skills/{result.Name}/{result.Version}/SKILL.md"
            });
    }

    /// <summary>
    /// Delete a skill version.
    /// </summary>
    [HttpDelete("{name}/{version}")]
    public async Task<IActionResult> DeleteVersion(string name, string version, CancellationToken ct)
    {
        var skill = await _repository.GetSkillByNameAsync(name, ct);
        if (skill is null)
            return NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var deleted = await _repository.DeleteVersionAsync(skill.Id, version, ct);
        if (!deleted)
            return NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found." });

        return NoContent();
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
