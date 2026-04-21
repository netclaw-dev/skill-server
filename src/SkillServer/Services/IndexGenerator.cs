// -----------------------------------------------------------------------
// <copyright file="IndexGenerator.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using SkillServer.Data;
using SkillServer.Models;

namespace SkillServer.Services;

/// <summary>
/// Generates RFC-compliant skill discovery index.
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
        var latestVersions = await _repository.GetAllLatestVersionsWithMetadataAsync(ct: ct);

        var entries = new List<RfcSkillEntry>(latestVersions.Count);

        foreach (var v in latestVersions)
        {
            var files = await _repository.GetFilesAsync(v.Id, ct);

            entries.Add(new RfcSkillEntry
            {
                Name = v.SkillName,
                Type = v.SkillType,
                Description = v.Description,
                Url = v.SkillType == SkillTypes.SkillMd
                    ? $"{baseUrl}/skills/{v.SkillName}/{v.Version}/SKILL.md"
                    : $"{baseUrl}/skills/{v.SkillName}/{v.Version}/archive.tar.gz",
                Digest = Sha256Digest.Create(v.Sha256).Value,
                Version = v.Version,
                Resources = files.Count > 0
                    ? files.Select(f => new RfcResourceEntry
                    {
                        Path = f.RelativePath,
                        Digest = Sha256Digest.Create(f.Sha256).Value,
                        Url = $"{baseUrl}/skills/{v.SkillName}/{v.Version}/{f.RelativePath}"
                    }).ToList()
                    : null
            });
        }

        _logger.LogDebug("Generated RFC index with {Count} skills", entries.Count);

        return new RfcSkillIndex { Skills = entries };
    }

    private string GetBaseUrl()
    {
        var baseUrl = _configuration["SkillServer:BaseUrl"] ?? "http://localhost:8080";
        return baseUrl.TrimEnd('/');
    }
}
