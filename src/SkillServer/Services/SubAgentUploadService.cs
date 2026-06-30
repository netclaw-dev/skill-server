// -----------------------------------------------------------------------
// <copyright file="SubAgentUploadService.cs" company="Petabridge, LLC">
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

public sealed partial class SubAgentUploadService
{
    private readonly SubAgentRepository _repository;
    private readonly BlobStorage _blobStorage;
    private readonly ILogger<SubAgentUploadService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public SubAgentUploadService(
        SubAgentRepository repository,
        BlobStorage blobStorage,
        ILogger<SubAgentUploadService> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<SubAgentUploadResult> UploadSubAgentAsync(
        SkillName name,
        SkillVersionString version,
        Stream content,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(content);
        var agentMdContent = await reader.ReadToEndAsync(ct);

        var parsed = Parse(agentMdContent);
        if (parsed is null)
            return SubAgentUploadResult.Failed("Invalid agent.md: missing or invalid YAML frontmatter.");

        var frontmatter = parsed.Value.Frontmatter;
        if (string.IsNullOrWhiteSpace(frontmatter.Name))
            return SubAgentUploadResult.Failed("agent.md must have a name in frontmatter.");

        if (!SkillName.TryCreate(frontmatter.Name, out var frontmatterName))
            return SubAgentUploadResult.Failed("Invalid sub-agent name. Must be 1-64 lowercase alphanumeric characters and hyphens.");

        if (!frontmatterName.Value.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase))
            return SubAgentUploadResult.Failed($"Frontmatter name '{frontmatter.Name}' does not match upload name '{name.Value}'.");

        if (string.IsNullOrWhiteSpace(frontmatter.Description))
            return SubAgentUploadResult.Failed("agent.md must have a description in frontmatter.");

        if (string.IsNullOrWhiteSpace(parsed.Value.Body))
            return SubAgentUploadResult.Failed("agent.md must have a non-empty prompt body.");

        if (!TryNormalizeModelRole(frontmatter.ModelRole, out var modelRole))
            return SubAgentUploadResult.Failed("Invalid modelRole. Must be 'Compaction' or 'Main'.");

        if (!TryNormalizeVisibility(frontmatter.Visibility, out var visibility))
            return SubAgentUploadResult.Failed("Invalid visibility. Must be 'user-facing' or 'internal'.");

        var timeoutSeconds = frontmatter.TimeoutSeconds ?? 60;
        if (timeoutSeconds is < 5 or > 600)
            return SubAgentUploadResult.Failed("Invalid timeoutSeconds. Must be between 5 and 600.");

        if (frontmatter.PrefillTimeoutSeconds is < 5 or > 3600)
            return SubAgentUploadResult.Failed("Invalid prefillTimeoutSeconds. Must be between 5 and 3600.");

        var subAgent = await _repository.GetSubAgentByNameAsync(name.Value, ct);
        long subAgentId;

        if (subAgent is null)
        {
            subAgentId = await _repository.CreateSubAgentAsync(name.Value, ct);
            _logger.LogInformation("Created new sub-agent {Name}", name.Value);
        }
        else
        {
            subAgentId = subAgent.Id;

            var existingVersion = await _repository.GetVersionAsync(subAgentId, version.Value, ct);
            if (existingVersion is not null)
                return SubAgentUploadResult.DuplicateVersion($"Version {version.Value} already exists for sub-agent {name.Value}.");
        }

        var contentBytes = Encoding.UTF8.GetBytes(agentMdContent);
        var (digest, sizeBytes) = await _blobStorage.StoreAsync(contentBytes, ct);
        var parsedDigest = Sha256Digest.Create(digest);

        await _repository.CreateVersionAsync(
            subAgentId,
            version.Value,
            frontmatter.Description,
            modelRole,
            timeoutSeconds,
            frontmatter.PrefillTimeoutSeconds,
            visibility,
            frontmatter.EmitStructuredFindings ?? false,
            parsedDigest.Value,
            sizeBytes,
            ct);

        await _repository.UpdateSubAgentTimestampAsync(subAgentId, ct);

        _logger.LogInformation("Uploaded sub-agent {Name} version {Version} ({Digest})", name.Value, version.Value, parsedDigest.Value);

        return SubAgentUploadResult.Succeeded(name, version, parsedDigest);
    }

    private ParsedSubAgent? Parse(string content)
    {
        var match = FrontmatterRegex().Match(content);
        if (!match.Success)
            return null;

        try
        {
            var frontmatter = _yamlDeserializer.Deserialize<SubAgentFrontmatter>(match.Groups[1].Value);
            if (frontmatter is null)
                return null;

            return new ParsedSubAgent(frontmatter, match.Groups[2].Value.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse agent.md frontmatter");
            return null;
        }
    }

    private static bool TryNormalizeModelRole(string? value, out string modelRole)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            modelRole = SubAgentModelRoles.Compaction;
            return true;
        }

        if (value.Equals(SubAgentModelRoles.Compaction, StringComparison.OrdinalIgnoreCase))
        {
            modelRole = SubAgentModelRoles.Compaction;
            return true;
        }

        if (value.Equals(SubAgentModelRoles.Main, StringComparison.OrdinalIgnoreCase))
        {
            modelRole = SubAgentModelRoles.Main;
            return true;
        }

        modelRole = "";
        return false;
    }

    private static bool TryNormalizeVisibility(string? value, out string visibility)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            visibility = SubAgentVisibility.UserFacing;
            return true;
        }

        if (value.Equals(SubAgentVisibility.UserFacing, StringComparison.OrdinalIgnoreCase) ||
            value.Equals("UserFacing", StringComparison.OrdinalIgnoreCase))
        {
            visibility = SubAgentVisibility.UserFacing;
            return true;
        }

        if (value.Equals(SubAgentVisibility.Internal, StringComparison.OrdinalIgnoreCase))
        {
            visibility = SubAgentVisibility.Internal;
            return true;
        }

        visibility = "";
        return false;
    }

    [GeneratedRegex(@"^---\s*\r?\n(.*?)\r?\n---\s*(?:\r?\n)?(.*)$", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}

public sealed record SubAgentUploadResult
{
    public required bool Success { get; init; }
    public SkillName? Name { get; init; }
    public SkillVersionString? Version { get; init; }
    public Sha256Digest? Digest { get; init; }
    public string? Error { get; init; }
    public bool IsDuplicateVersion { get; init; }

    public static SubAgentUploadResult Succeeded(SkillName name, SkillVersionString version, Sha256Digest digest) =>
        new() { Success = true, Name = name, Version = version, Digest = digest };

    public static SubAgentUploadResult Failed(string error) =>
        new() { Success = false, Error = error };

    public static SubAgentUploadResult DuplicateVersion(string error) =>
        new() { Success = false, Error = error, IsDuplicateVersion = true };
}

internal readonly record struct ParsedSubAgent(SubAgentFrontmatter Frontmatter, string Body);

internal sealed class SubAgentFrontmatter
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ModelRole { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? PrefillTimeoutSeconds { get; set; }
    public string? Visibility { get; set; }
    public bool? EmitStructuredFindings { get; set; }
}
