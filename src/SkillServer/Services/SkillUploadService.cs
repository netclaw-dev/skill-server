// -----------------------------------------------------------------------
// <copyright file="SkillUploadService.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text;
using System.Text.RegularExpressions;
using SkillServer.Data;
using SkillServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SkillServer.Services;

/// <summary>
/// Handles skill upload, parsing, and storage.
/// </summary>
public sealed partial class SkillUploadService
{
    private readonly SkillRepository _repository;
    private readonly BlobStorage _blobStorage;
    private readonly ILogger<SkillUploadService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public SkillUploadService(
        SkillRepository repository,
        BlobStorage blobStorage,
        ILogger<SkillUploadService> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Uploads a skill from a SKILL.md file.
    /// </summary>
    public async Task<SkillUploadResult> UploadSkillMdAsync(
        SkillName name,
        SkillVersionString version,
        Stream content,
        string? category = null,
        CancellationToken ct = default)
    {

        // Read content
        using var reader = new StreamReader(content);
        var skillMdContent = await reader.ReadToEndAsync(ct);

        // Parse frontmatter
        var frontmatter = ParseFrontmatter(skillMdContent);
        if (frontmatter is null)
        {
            return SkillUploadResult.Failed("Invalid SKILL.md: missing or invalid YAML frontmatter.");
        }

        // Validate frontmatter name matches
        if (!string.IsNullOrEmpty(frontmatter.Name) && !frontmatter.Name.Equals(name.Value, StringComparison.OrdinalIgnoreCase))
        {
            return SkillUploadResult.Failed($"Frontmatter name '{frontmatter.Name}' does not match upload name '{name.Value}'.");
        }

        var description = frontmatter.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            return SkillUploadResult.Failed("SKILL.md must have a description in frontmatter.");
        }

        string? routesToSubagent = null;
        if (frontmatter.Metadata?.TryGetValue("subagent", out var subagent) == true)
        {
            if (!SkillName.TryCreate(subagent, out var subagentName))
                return SkillUploadResult.Failed("Invalid metadata.subagent value. Must be a valid lowercase kebab-case sub-agent name.");

            routesToSubagent = subagentName.Value.Value;
        }

        // Store the blob
        var contentBytes = Encoding.UTF8.GetBytes(skillMdContent);
        var (digest, sizeBytes) = await _blobStorage.StoreAsync(contentBytes, ct);

        // Get or create skill
        var skill = await _repository.GetSkillByNameAsync(name.Value, ct);
        long skillId;

        if (skill is null)
        {
            skillId = await _repository.CreateSkillAsync(name.Value, ct);
            _logger.LogInformation("Created new skill {Name}", name.Value);
        }
        else
        {
            skillId = skill.Id;

            // Check if version already exists
            var existingVersion = await _repository.GetVersionAsync(skillId, version.Value, ct);
            if (existingVersion is not null)
            {
                return SkillUploadResult.DuplicateVersion($"Version {version.Value} already exists for skill {name.Value}.");
            }
        }

        // Create version
        var parsedDigest = Sha256Digest.Create(digest);
        var versionId = await _repository.CreateVersionAsync(
            skillId,
            version.Value,
            description,
            category ?? frontmatter.Metadata?.GetValueOrDefault("category"),
            SkillTypes.SkillMd,
            parsedDigest.Value,
            sizeBytes,
            ct,
            routesToSubagent: routesToSubagent);

        await _repository.UpdateSkillTimestampAsync(skillId, ct);

        _logger.LogInformation("Uploaded skill {Name} version {Version} ({Digest})", name.Value, version.Value, parsedDigest.Value);

        return SkillUploadResult.Succeeded(name, version, parsedDigest);
    }

    /// <summary>
    /// Uploads a skill with additional resource files.
    /// </summary>
    public async Task<SkillUploadResult> UploadSkillWithResourcesAsync(
        SkillName name,
        SkillVersionString version,
        Stream skillMdContent,
        IReadOnlyList<SkillResourceUpload> resources,
        string? category = null,
        CancellationToken ct = default)
    {
        if (resources.Count == 0)
            return await UploadSkillMdAsync(name, version, skillMdContent, category, ct);

        using var skillMdBuffer = new MemoryStream();
        await skillMdContent.CopyToAsync(skillMdBuffer, ct);
        var skillMdBytes = skillMdBuffer.ToArray();

        var resourceContents = new List<SkillArchiveResource>(resources.Count);
        foreach (var resource in resources)
        {
            using var resourceBuffer = new MemoryStream();
            await resource.Content.CopyToAsync(resourceBuffer, ct);
            resourceContents.Add(new SkillArchiveResource(resource.Path, resourceBuffer.ToArray(), resource.UnixMode));
        }

        // First upload the SKILL.md
        using var skillMdUploadStream = new MemoryStream(skillMdBytes, writable: false);
        var result = await UploadSkillMdAsync(name, version, skillMdUploadStream, category, ct);
        if (!result.Success)
            return result;

        // Get the version ID
        var skill = await _repository.GetSkillByNameAsync(name.Value, ct);
        if (skill is null) return SkillUploadResult.Failed("Skill not found after upload.");

        var skillVersion = await _repository.GetVersionAsync(skill.Id, version.Value, ct);
        if (skillVersion is null) return SkillUploadResult.Failed("Version not found after upload.");

        // Store resource files
        foreach (var resource in resourceContents)
        {
            var (digest, sizeBytes) = await _blobStorage.StoreAsync(resource.Content, ct);
            var parsedDigest = Sha256Digest.Create(digest);
            await _repository.AddFileAsync(skillVersion.Id, resource.Path.Value, parsedDigest.Value, sizeBytes, ct, resource.UnixMode);
            _logger.LogDebug("Added resource {Path} ({Digest})", resource.Path.Value, parsedDigest.Value);
        }

        var archiveBytes = SkillArchiveBuilder.BuildZip(skillMdBytes, resourceContents);
        var (archiveDigest, archiveSizeBytes) = await _blobStorage.StoreAsync(archiveBytes, ct);
        var parsedArchiveDigest = Sha256Digest.Create(archiveDigest);
        await _repository.UpdateVersionArtifactAsync(
            skillVersion.Id, SkillTypes.Archive, parsedArchiveDigest.Value, archiveSizeBytes, ct);

        _logger.LogDebug("Added archive artifact for {Name} {Version} ({Digest})", name.Value, version.Value, parsedArchiveDigest.Value);

        return result;
    }

    private SkillFrontmatter? ParseFrontmatter(string content)
    {
        var match = FrontmatterRegex().Match(content);
        if (!match.Success)
            return null;

        try
        {
            var yaml = match.Groups[1].Value;
            return _yamlDeserializer.Deserialize<SkillFrontmatter>(yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse SKILL.md frontmatter");
            return null;
        }
    }

    [GeneratedRegex(@"^---\s*\n(.*?)\n---", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}

public readonly record struct SkillResourceUpload(ResourcePath Path, Stream Content, int? UnixMode);

/// <summary>
/// Result of a skill upload operation.
/// </summary>
public sealed record SkillUploadResult
{
    public required bool Success { get; init; }
    public SkillName? Name { get; init; }
    public SkillVersionString? Version { get; init; }
    public Sha256Digest? Digest { get; init; }
    public string? Error { get; init; }
    public bool IsDuplicateVersion { get; init; }

    public static SkillUploadResult Succeeded(SkillName name, SkillVersionString version, Sha256Digest digest) =>
        new() { Success = true, Name = name, Version = version, Digest = digest };

    public static SkillUploadResult Failed(string error) =>
        new() { Success = false, Error = error };

    public static SkillUploadResult DuplicateVersion(string error) =>
        new() { Success = false, Error = error, IsDuplicateVersion = true };
}

/// <summary>
/// SKILL.md frontmatter structure.
/// </summary>
internal sealed class SkillFrontmatter
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? License { get; set; }
    public string? Compatibility { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
