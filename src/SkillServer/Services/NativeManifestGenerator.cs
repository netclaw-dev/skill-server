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
    private const string CurrentApiVersion = "v1";
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
            ApiVersion = CurrentApiVersion,
            Versions = new Dictionary<string, NativeVersionLinks>
            {
                {
                    CurrentApiVersion, new NativeVersionLinks
                    {
                        Self = Link("/manifest.json"),
                        Skills = Link("/skills/v1/index.json"),
                        SubAgents = Link("/subagents/v1/index.json"),
                        SkillSearch = Link("/api/v1/skills"),
                        SubAgentSearch = Link("/api/v1/subagents")
                    }
                }
            }
        };

        return Task.FromResult(manifest);
    }

    public Task<NativeSkillCollectionIndex> GenerateSkillIndexAsync(CancellationToken ct = default)
    {
        var index = new NativeSkillCollectionIndex
        {
            Links = SelfLinks("/skills/v1/index.json"),
            Pages = [new NativeManifestPageLink
            {
                Range = SkillsPageRange,
                Href = "/skills/v1/pages/all.json"
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
                Href = $"/skills/v1/{latest.SkillName}/index.json"
            });
        }

        return new NativeSkillCollectionPage
        {
            Range = SkillsPageRange,
            Items = items,
            Links = SelfLinks("/skills/v1/pages/all.json")
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
                Href = $"/skills/v1/{skill.Name}/versions/{v.Version}.json"
            }).ToList(),
            Links = SelfLinks($"/skills/v1/{skill.Name}/index.json")
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
                    Href = $"/subagents/v1/{skillVersion.RoutesToSubagent}/index.json"
                },
            Links = SelfLinks($"/skills/v1/{skill.Name}/versions/{skillVersion.Version}.json")
        };
    }

    public Task<NativeSubAgentCollectionIndex> GenerateSubAgentIndexAsync(CancellationToken ct = default)
    {
        var index = new NativeSubAgentCollectionIndex
        {
            Links = SelfLinks("/subagents/v1/index.json"),
            Pages = [new NativeManifestPageLink
            {
                Range = SubAgentsPageRange,
                Href = "/subagents/v1/pages/all.json"
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
                Href = $"/subagents/v1/{latest.SubAgentName}/index.json"
            });
        }

        return new NativeSubAgentCollectionPage
        {
            Range = SubAgentsPageRange,
            Items = items,
            Links = SelfLinks("/subagents/v1/pages/all.json")
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
                Href = $"/subagents/v1/{subAgent.Name}/versions/{v.Version}.json"
            }).ToList(),
            Links = SelfLinks($"/subagents/v1/{subAgent.Name}/index.json")
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
            Links = SelfLinks($"/subagents/v1/{subAgent.Name}/versions/{subAgentVersion.Version}.json")
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
