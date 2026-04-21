using SkillServer.Data;
using SkillServer.Models;

namespace SkillServer.Services;

/// <summary>
/// Generates RFC and NetClaw format indexes from stored skills.
/// </summary>
public sealed class IndexGenerator
{
    private readonly SkillRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexGenerator> _logger;

    public IndexGenerator(
        SkillRepository repository,
        IConfiguration configuration,
        ILogger<IndexGenerator> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates the RFC-compliant index for /.well-known/agent-skills/index.json
    /// </summary>
    public async Task<RfcSkillIndex> GenerateRfcIndexAsync(CancellationToken ct = default)
    {
        var baseUrl = GetBaseUrl();
        var skills = await _repository.GetAllSkillsAsync(ct);
        var entries = new List<RfcSkillEntry>();

        foreach (var skill in skills)
        {
            var version = await _repository.GetLatestVersionAsync(skill.Id, ct);
            if (version is null) continue;

            var files = await _repository.GetFilesAsync(version.Id, ct);

            var entry = new RfcSkillEntry
            {
                Name = skill.Name,
                Type = version.SkillType == SkillType.SkillMd ? "skill-md" : "archive",
                Description = version.Description,
                Url = version.SkillType == SkillType.SkillMd
                    ? $"{baseUrl}/skills/{skill.Name}/{version.Version}/SKILL.md"
                    : $"{baseUrl}/skills/{skill.Name}/{version.Version}/archive.tar.gz",
                Digest = version.Sha256.StartsWith("sha256:") ? version.Sha256 : $"sha256:{version.Sha256}",
                Version = version.Version,
                Resources = files.Count > 0
                    ? files.Select(f => new RfcResourceEntry
                    {
                        Path = f.RelativePath,
                        Digest = f.Sha256.StartsWith("sha256:") ? f.Sha256 : $"sha256:{f.Sha256}",
                        Url = $"{baseUrl}/skills/{skill.Name}/{version.Version}/{f.RelativePath}"
                    }).ToList()
                    : null
            };

            entries.Add(entry);
        }

        _logger.LogDebug("Generated RFC index with {Count} skills", entries.Count);

        return new RfcSkillIndex { Skills = entries };
    }

    /// <summary>
    /// Generates the NetClaw-compatible manifest at /manifest.json
    /// </summary>
    public async Task<NetclawManifest> GenerateNetclawManifestAsync(CancellationToken ct = default)
    {
        var baseUrl = GetBaseUrl();
        var skills = await _repository.GetAllSkillsAsync(ct);
        var entries = new List<NetclawSkillEntry>();
        var latestUpdate = DateTimeOffset.MinValue;

        foreach (var skill in skills)
        {
            var version = await _repository.GetLatestVersionAsync(skill.Id, ct);
            if (version is null) continue;

            if (skill.UpdatedAt > latestUpdate)
                latestUpdate = skill.UpdatedAt;

            var files = await _repository.GetFilesAsync(version.Id, ct);

            var entry = new NetclawSkillEntry
            {
                Name = skill.Name,
                Version = version.Version,
                Sha256 = version.Sha256.StartsWith("sha256:")
                    ? version.Sha256[7..]
                    : version.Sha256,
                SizeBytes = version.SizeBytes,
                Url = version.SkillType == SkillType.SkillMd
                    ? $"{baseUrl}/skills/{skill.Name}/{version.Version}/SKILL.md"
                    : $"{baseUrl}/skills/{skill.Name}/{version.Version}/archive.tar.gz",
                Category = version.Category,
                Description = version.Description,
                Files = files.Count > 0
                    ? files.Select(f => new NetclawFileEntry
                    {
                        Path = f.RelativePath,
                        Sha256 = f.Sha256.StartsWith("sha256:") ? f.Sha256[7..] : f.Sha256,
                        SizeBytes = f.SizeBytes,
                        Url = $"{baseUrl}/skills/{skill.Name}/{version.Version}/{f.RelativePath}"
                    }).ToList()
                    : null
            };

            entries.Add(entry);
        }

        _logger.LogDebug("Generated NetClaw manifest with {Count} skills", entries.Count);

        return new NetclawManifest
        {
            UpdatedAt = latestUpdate == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : latestUpdate,
            Skills = entries
        };
    }

    private string GetBaseUrl()
    {
        var baseUrl = _configuration["SkillServer:BaseUrl"] ?? "http://localhost:8080";
        return baseUrl.TrimEnd('/');
    }
}
