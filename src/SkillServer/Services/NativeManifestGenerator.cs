// -----------------------------------------------------------------------
// <copyright file="NativeManifestGenerator.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using SkillServer.Data;
using SkillServer.Models;

namespace SkillServer.Services;

public sealed class NativeManifestGenerator
{
    private const string SkillsPageRange = "all";

    private readonly SkillRepository _repository;
    private readonly IConfiguration _configuration;

    public NativeManifestGenerator(SkillRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    public Task<NativeRootManifest> GenerateRootAsync(CancellationToken ct = default)
    {
        var manifest = new NativeRootManifest
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Links = new NativeRootManifestLinks
            {
                Self = Link("/manifest.json"),
                RfcSkills = Link("/.well-known/agent-skills/index.json"),
                Skills = Link("/manifest/skills/index.json")
            }
        };

        return Task.FromResult(manifest);
    }

    public Task<NativeSkillCollectionIndex> GenerateSkillIndexAsync(CancellationToken ct = default)
    {
        var index = new NativeSkillCollectionIndex
        {
            Links = SelfLinks("/manifest/skills/index.json"),
            Pages = [new NativeManifestPageLink
            {
                Range = SkillsPageRange,
                Href = "/manifest/skills/pages/all.json"
            }]
        };

        return Task.FromResult(index);
    }

    public async Task<NativeSkillCollectionPage?> GenerateSkillPageAsync(string page, CancellationToken ct = default)
    {
        if (!page.Equals(SkillsPageRange, StringComparison.OrdinalIgnoreCase))
            return null;

        var latestVersions = await _repository.GetAllLatestVersionsWithMetadataAsync(ct: ct);
        var items = new List<NativeSkillPageItem>(latestVersions.Count);

        foreach (var latest in latestVersions)
        {
            var versions = await _repository.GetAllVersionsAsync(latest.SkillId, ct);
            if (versions.Count == 0)
                continue;

            var oldest = versions[^1];
            items.Add(new NativeSkillPageItem
            {
                Name = latest.SkillName,
                LatestVersion = latest.Version,
                VersionRange = new NativeVersionRange
                {
                    Min = oldest.Version,
                    Max = latest.Version,
                    Count = versions.Count
                },
                Href = $"/manifest/skills/{latest.SkillName}/index.json"
            });
        }

        return new NativeSkillCollectionPage
        {
            Range = SkillsPageRange,
            Items = items,
            Links = SelfLinks("/manifest/skills/pages/all.json")
        };
    }

    public async Task<NativeSkillIdentityIndex?> GenerateSkillIdentityAsync(string skillName, CancellationToken ct = default)
    {
        var skill = await _repository.GetSkillByNameAsync(skillName, ct);
        if (skill is null)
            return null;

        var versions = await _repository.GetAllVersionsAsync(skill.Id, ct);
        if (versions.Count == 0)
            return null;

        var latest = versions.FirstOrDefault(v => v.IsLatest) ?? versions[0];
        return new NativeSkillIdentityIndex
        {
            Name = skill.Name,
            LatestVersion = latest.Version,
            Versions = versions.Select(v => new NativeSkillVersionLink
            {
                Version = v.Version,
                PublishedAt = v.PublishedAt,
                Digest = Sha256Digest.Create(v.ArtifactSha256).Value,
                Href = $"/manifest/skills/{skill.Name}/versions/{v.Version}.json"
            }).ToList(),
            Links = SelfLinks($"/manifest/skills/{skill.Name}/index.json")
        };
    }

    public async Task<NativeSkillVersionDetail?> GenerateSkillVersionAsync(string skillName, string version, CancellationToken ct = default)
    {
        var skill = await _repository.GetSkillByNameAsync(skillName, ct);
        if (skill is null)
            return null;

        var skillVersion = await _repository.GetVersionAsync(skill.Id, version, ct);
        if (skillVersion is null)
            return null;

        return new NativeSkillVersionDetail
        {
            Artifact = CreateArtifact(skill.Name, skillVersion),
            RoutesToSubagent = skillVersion.RoutesToSubagent is null
                ? null
                : new NativeSubagentRoute
                {
                    Name = skillVersion.RoutesToSubagent,
                    Href = $"/manifest/subagents/{skillVersion.RoutesToSubagent}/index.json"
                },
            Links = SelfLinks($"/manifest/skills/{skill.Name}/versions/{skillVersion.Version}.json")
        };
    }

    private NativeSkillArtifact CreateArtifact(string skillName, SkillVersion version)
    {
        var baseUrl = GetBaseUrl();
        return new NativeSkillArtifact
        {
            Name = skillName,
            Version = version.Version,
            Type = version.SkillType,
            Description = version.Description,
            Url = version.SkillType == SkillTypes.SkillMd
                ? $"{baseUrl}/skills/{skillName}/{version.Version}/SKILL.md"
                : $"{baseUrl}/skills/{skillName}/{version.Version}/archive.zip",
            Digest = Sha256Digest.Create(version.ArtifactSha256).Value
        };
    }

    private string GetBaseUrl()
    {
        var baseUrl = _configuration["SkillServer:BaseUrl"] ?? "http://localhost:8080";
        return baseUrl.TrimEnd('/');
    }

    private static NativeManifestSelfLinks SelfLinks(string href) => new() { Self = Link(href) };

    private static NativeManifestLink Link(string href) => new() { Href = href };
}
