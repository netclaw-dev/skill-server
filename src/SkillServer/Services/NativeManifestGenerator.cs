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
    private const string SubAgentsPageRange = "all";

    private readonly SkillRepository _skillRepository;
    private readonly SubAgentRepository _subAgentRepository;
    private readonly IConfiguration _configuration;

    public NativeManifestGenerator(
        SkillRepository skillRepository,
        SubAgentRepository subAgentRepository,
        IConfiguration configuration)
    {
        _skillRepository = skillRepository;
        _subAgentRepository = subAgentRepository;
        _configuration = configuration;
    }

    public Task<NativeRootManifest> GenerateRootAsync(CancellationToken ct = default)
    {
        var manifest = new NativeRootManifest
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            ApiVersion = "v1",
            Links = new NativeRootManifestLinks
            {
                Self = Link("/api/v1/manifest.json"),
                RfcSkills = Link("/.well-known/agent-skills/index.json"),
                Skills = Link("/api/v1/manifest/skills/index.json"),
                SubAgents = Link("/api/v1/manifest/subagents/index.json"),
                ApiBase = Link("/api/v1")
            }
        };

        return Task.FromResult(manifest);
    }

    public Task<NativeSkillCollectionIndex> GenerateSkillIndexAsync(CancellationToken ct = default)
    {
        var index = new NativeSkillCollectionIndex
        {
            Links = SelfLinks("/api/v1/manifest/skills/index.json"),
            Pages = [new NativeManifestPageLink
            {
                Range = SkillsPageRange,
                Href = "/api/v1/manifest/skills/pages/all.json"
            }]
        };

        return Task.FromResult(index);
    }

    public async Task<NativeSkillCollectionPage?> GenerateSkillPageAsync(string page, CancellationToken ct = default)
    {
        if (!page.Equals(SkillsPageRange, StringComparison.OrdinalIgnoreCase))
            return null;

        var latestVersions = await _skillRepository.GetAllLatestVersionsWithMetadataAsync(ct: ct);
        var items = new List<NativeSkillPageItem>(latestVersions.Count);

        foreach (var latest in latestVersions)
        {
            var versions = await _skillRepository.GetAllVersionsAsync(latest.SkillId, ct);
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
                Href = $"/api/v1/manifest/skills/{latest.SkillName}/index.json"
            });
        }

        return new NativeSkillCollectionPage
        {
            Range = SkillsPageRange,
            Items = items,
            Links = SelfLinks("/api/v1/manifest/skills/pages/all.json")
        };
    }

    public async Task<NativeSkillIdentityIndex?> GenerateSkillIdentityAsync(string skillName, CancellationToken ct = default)
    {
        var skill = await _skillRepository.GetSkillByNameAsync(skillName, ct);
        if (skill is null)
            return null;

        var versions = await _skillRepository.GetAllVersionsAsync(skill.Id, ct);
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
                Href = $"/api/v1/manifest/skills/{skill.Name}/versions/{v.Version}.json"
            }).ToList(),
            Links = SelfLinks($"/api/v1/manifest/skills/{skill.Name}/index.json")
        };
    }

    public async Task<NativeSkillVersionDetail?> GenerateSkillVersionAsync(string skillName, string version, CancellationToken ct = default)
    {
        var skill = await _skillRepository.GetSkillByNameAsync(skillName, ct);
        if (skill is null)
            return null;

        var skillVersion = await _skillRepository.GetVersionAsync(skill.Id, version, ct);
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
                    Href = $"/api/v1/manifest/subagents/{skillVersion.RoutesToSubagent}/index.json"
                },
            Links = SelfLinks($"/api/v1/manifest/skills/{skill.Name}/versions/{skillVersion.Version}.json")
        };
    }

    public Task<NativeSubAgentCollectionIndex> GenerateSubAgentIndexAsync(CancellationToken ct = default)
    {
        var index = new NativeSubAgentCollectionIndex
        {
            Links = SelfLinks("/api/v1/manifest/subagents/index.json"),
            Pages = [new NativeManifestPageLink
            {
                Range = SubAgentsPageRange,
                Href = "/api/v1/manifest/subagents/pages/all.json"
            }]
        };

        return Task.FromResult(index);
    }

    public async Task<NativeSubAgentCollectionPage?> GenerateSubAgentPageAsync(string page, CancellationToken ct = default)
    {
        if (!page.Equals(SubAgentsPageRange, StringComparison.OrdinalIgnoreCase))
            return null;

        var latestVersions = await _subAgentRepository.GetAllLatestVersionsWithMetadataAsync(ct);
        var items = new List<NativeSubAgentPageItem>(latestVersions.Count);

        foreach (var latest in latestVersions)
        {
            var versions = await _subAgentRepository.GetAllVersionsAsync(latest.SubAgentId, ct);
            if (versions.Count == 0)
                continue;

            var oldest = versions[^1];
            items.Add(new NativeSubAgentPageItem
            {
                Name = latest.SubAgentName,
                LatestVersion = latest.Version,
                VersionRange = new NativeVersionRange
                {
                    Min = oldest.Version,
                    Max = latest.Version,
                    Count = versions.Count
                },
                Href = $"/api/v1/manifest/subagents/{latest.SubAgentName}/index.json"
            });
        }

        return new NativeSubAgentCollectionPage
        {
            Range = SubAgentsPageRange,
            Items = items,
            Links = SelfLinks("/api/v1/manifest/subagents/pages/all.json")
        };
    }

    public async Task<NativeSubAgentIdentityIndex?> GenerateSubAgentIdentityAsync(string subAgentName, CancellationToken ct = default)
    {
        var subAgent = await _subAgentRepository.GetSubAgentByNameAsync(subAgentName, ct);
        if (subAgent is null)
            return null;

        var versions = await _subAgentRepository.GetAllVersionsAsync(subAgent.Id, ct);
        if (versions.Count == 0)
            return null;

        var latest = versions.FirstOrDefault(v => v.IsLatest) ?? versions[0];
        return new NativeSubAgentIdentityIndex
        {
            Name = subAgent.Name,
            LatestVersion = latest.Version,
            Versions = versions.Select(v => new NativeSubAgentVersionLink
            {
                Version = v.Version,
                PublishedAt = v.PublishedAt,
                Digest = Sha256Digest.Create(v.Sha256).Value,
                Href = $"/api/v1/manifest/subagents/{subAgent.Name}/versions/{v.Version}.json"
            }).ToList(),
            Links = SelfLinks($"/api/v1/manifest/subagents/{subAgent.Name}/index.json")
        };
    }

    public async Task<NativeSubAgentVersionDetail?> GenerateSubAgentVersionAsync(string subAgentName, string version, CancellationToken ct = default)
    {
        var subAgent = await _subAgentRepository.GetSubAgentByNameAsync(subAgentName, ct);
        if (subAgent is null)
            return null;

        var subAgentVersion = await _subAgentRepository.GetVersionAsync(subAgent.Id, version, ct);
        if (subAgentVersion is null)
            return null;

        var baseUrl = GetBaseUrl();
        return new NativeSubAgentVersionDetail
        {
            Name = subAgent.Name,
            Version = subAgentVersion.Version,
            Description = subAgentVersion.Description,
            Url = $"{baseUrl}/api/v1/subagents/{subAgent.Name}/{subAgentVersion.Version}/agent.md",
            Digest = Sha256Digest.Create(subAgentVersion.Sha256).Value,
            Links = SelfLinks($"/api/v1/manifest/subagents/{subAgent.Name}/versions/{subAgentVersion.Version}.json")
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
                ? $"{baseUrl}/api/v1/skills/{skillName}/{version.Version}/SKILL.md"
                : $"{baseUrl}/api/v1/skills/{skillName}/{version.Version}/archive.zip",
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
