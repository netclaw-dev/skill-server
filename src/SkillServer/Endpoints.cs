// -----------------------------------------------------------------------
// <copyright file="Endpoints.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;
using SkillServer.Data;
using SkillServer.Models;
using SkillServer.Services;

namespace SkillServer;

public static class Endpoints
{
    public static WebApplication MapSkillServerEndpoints(this WebApplication app)
    {
        app.MapDiscoveryEndpoints();
        app.MapManifestEndpoints();
        app.MapV1Api();
        app.MapHealthEndpoints();
        return app;
    }

    private static void MapV1Api(this WebApplication app)
    {
        app.MapAppInfoEndpoints();
        app.MapSkillEndpoints();
        app.MapSubAgentEndpoints();
        app.MapBlobEndpoints();
        app.MapApiKeyEndpoints();
    }

    private static void MapAppInfoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/info", () =>
        {
            var assembly = typeof(Endpoints).Assembly;
            var assemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown";
            var version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assemblyVersion;

            return Results.Json(new AppInfoResponse
            {
                Version = version,
                AssemblyVersion = assemblyVersion
            }, SkillServerJsonContext.Default.AppInfoResponse);
        });
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

    private static void MapManifestEndpoints(this WebApplication app)
    {
        // Root manifest — discovery entry point for HATEOAS clients
        // Clients hit /manifest.json first, read versions dict, then follow resource links
        app.MapGet("/manifest.json", async (
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var manifest = await manifestGenerator.GenerateRootAsync(ct);
            return Results.Json(manifest, SkillServerJsonContext.Default.NativeRootManifest);
        });

        // Skill manifest endpoints — /skills/v1/*
        var skillManifest = app.MapGroup("/skills/v1");

        skillManifest.MapGet("/index.json", async (
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var index = await manifestGenerator.GenerateSkillIndexAsync(ct);
            return Results.Json(index, SkillServerJsonContext.Default.NativeSkillCollectionIndex);
        });

        skillManifest.MapGet("/pages/{page}.json", async (
            string page,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var skillPage = await manifestGenerator.GenerateSkillPageAsync(page, ct);
            return skillPage is null
                ? Results.NotFound()
                : Results.Json(skillPage, SkillServerJsonContext.Default.NativeSkillCollectionPage);
        });

        skillManifest.MapGet("/{skillName}/index.json", async (
            string skillName,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var skill = await manifestGenerator.GenerateSkillIdentityAsync(skillName, ct);
            return skill is null
                ? Results.NotFound()
                : Results.Json(skill, SkillServerJsonContext.Default.NativeSkillIdentityIndex);
        });

        skillManifest.MapGet("/{skillName}/versions/{version}.json", async (
            string skillName,
            string version,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var skillVersion = await manifestGenerator.GenerateSkillVersionAsync(skillName, version, ct);
            return skillVersion is null
                ? Results.NotFound()
                : Results.Json(skillVersion, SkillServerJsonContext.Default.NativeSkillVersionDetail);
        });

        // Subagent manifest endpoints — /subagents/v1/*
        var subagentManifest = app.MapGroup("/subagents/v1");

        subagentManifest.MapGet("/index.json", async (
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var index = await manifestGenerator.GenerateSubAgentIndexAsync(ct);
            return Results.Json(index, SkillServerJsonContext.Default.NativeSubAgentCollectionIndex);
        });

        subagentManifest.MapGet("/pages/{page}.json", async (
            string page,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var subAgentPage = await manifestGenerator.GenerateSubAgentPageAsync(page, ct);
            return subAgentPage is null
                ? Results.NotFound()
                : Results.Json(subAgentPage, SkillServerJsonContext.Default.NativeSubAgentCollectionPage);
        });

        subagentManifest.MapGet("/{subAgentName}/index.json", async (
            string subAgentName,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var subAgent = await manifestGenerator.GenerateSubAgentIdentityAsync(subAgentName, ct);
            return subAgent is null
                ? Results.NotFound()
                : Results.Json(subAgent, SkillServerJsonContext.Default.NativeSubAgentIdentityIndex);
        });

        subagentManifest.MapGet("/{subAgentName}/versions/{version}.json", async (
            string subAgentName,
            string version,
            NativeManifestGenerator manifestGenerator,
            CancellationToken ct) =>
        {
            var subAgentVersion = await manifestGenerator.GenerateSubAgentVersionAsync(subAgentName, version, ct);
            return subAgentVersion is null
                ? Results.NotFound()
                : Results.Json(subAgentVersion, SkillServerJsonContext.Default.NativeSubAgentVersionDetail);
        });
    }

    private static void MapSkillEndpoints(this WebApplication app)
    {
        var skills = app.MapGroup("/api/v1/skills");

        skills.MapGet("/", ListSkills);
        skills.MapGet("/{name}", GetSkill);
        skills.MapGet("/{name}/latest", GetLatestVersion);
        skills.MapGet("/{name}/{version}", GetVersion);
        skills.MapGet("/{name}/{version}/SKILL.md", DownloadSkillMd);
        skills.MapGet("/{name}/{version}/archive.zip", DownloadArchive);
        skills.MapGet("/{name}/{version}/{*path}", DownloadResource);
        skills.MapPost("/check-updates", CheckUpdates);
        skills.MapPost("/", UploadSkill).DisableAntiforgery().AddEndpointFilter<ApiKeyEndpointFilter>();
        skills.MapDelete("/{name}/{version}", DeleteVersion).AddEndpointFilter<ApiKeyEndpointFilter>();
    }

    private static void MapSubAgentEndpoints(this WebApplication app)
    {
        var subagents = app.MapGroup("/api/v1/subagents");

        subagents.MapGet("/", ListSubAgents);
        subagents.MapGet("/{name}", GetSubAgent);
        subagents.MapGet("/{name}/{version}", GetSubAgentVersion);
        subagents.MapGet("/{name}/{version}/agent.md", DownloadSubAgentMd);
        subagents.MapPost("/", UploadSubAgent).DisableAntiforgery().AddEndpointFilter<ApiKeyEndpointFilter>();
        subagents.MapDelete("/{name}/{version}", DeleteSubAgentVersion).AddEndpointFilter<ApiKeyEndpointFilter>();
    }

    private static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Json(
            new HealthResponse { Status = "healthy", Timestamp = DateTimeOffset.UtcNow },
            SkillServerJsonContext.Default.HealthResponse));
    }

    private static async Task<IResult> ListSkills(
        SkillRepository repository,
        string? q,
        int? skip,
        int? take,
        CancellationToken ct)
    {
        IReadOnlyList<SkillVersionWithMetadata> latestVersions;

        if (!string.IsNullOrWhiteSpace(q))
            latestVersions = await repository.SearchSkillsAsync(q, skip, take, ct);
        else
            latestVersions = await repository.GetAllLatestVersionsWithMetadataAsync(skip, take, ct);

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

    private static async Task<IResult> DownloadArchive(
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
        if (skillVersion is null || skillVersion.SkillType != SkillTypes.Archive)
            return Results.NotFound();

        var stream = blobStorage.GetBlob(skillVersion.ArtifactSha256);
        if (stream is null)
            return Results.NotFound();

        return Results.File(stream, "application/zip", "archive.zip");
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
        HttpRequest request,
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

        if (!TryParseResourceMetadata(request.Form, out var resourceMetadata, out var metadataError))
            return Results.BadRequest(metadataError);

        var resourceFiles = request.Form.Files.GetFiles("resources");
        var resources = new List<SkillResourceUpload>();
        var resourcePaths = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var resourceFile in resourceFiles)
            {
                if (!ResourcePath.TryCreate(resourceFile.FileName, out var resourcePath))
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Error = "invalid_resource_path",
                        Message = $"Invalid resource path: '{resourceFile.FileName}'. Must be a relative path in a subdirectory with no path traversal."
                    });
                }

                var resourcePathValue = resourcePath.Value.Value;
                resourcePaths.Add(resourcePathValue);
                resourceMetadata.TryGetValue(resourcePathValue, out var unixMode);
                resources.Add(new SkillResourceUpload(resourcePath.Value, resourceFile.OpenReadStream(), unixMode));
            }

            var unmatchedMetadataPath = resourceMetadata.Keys.FirstOrDefault(path => !resourcePaths.Contains(path));
            if (unmatchedMetadataPath is not null)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "invalid_resource_metadata",
                    Message = $"Resource metadata path '{unmatchedMetadataPath}' does not match any uploaded resource."
                });
            }

            await using var stream = file.OpenReadStream();
            var result = await uploadService.UploadSkillWithResourcesAsync(
                skillName.Value, skillVersion.Value, stream, resources, category, ct);

            return HandleUploadResult(result, configuration);
        }
        finally
        {
            foreach (var resource in resources)
                await resource.Content.DisposeAsync();
        }
    }

    private static bool TryParseResourceMetadata(
        IFormCollection form,
        out Dictionary<string, int?> metadata,
        out ErrorResponse? error)
    {
        metadata = new Dictionary<string, int?>(StringComparer.Ordinal);
        error = null;

        var raw = form["resourceMetadata"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        IReadOnlyList<SkillResourceUploadMetadata>? entries;
        try
        {
            entries = JsonSerializer.Deserialize(
                raw,
                SkillServerJsonContext.Default.IReadOnlyListSkillResourceUploadMetadata);
        }
        catch (JsonException ex)
        {
            error = new ErrorResponse
            {
                Error = "invalid_resource_metadata",
                Message = $"Invalid resourceMetadata JSON: {ex.Message}"
            };
            return false;
        }

        if (entries is null)
            return true;

        foreach (var entry in entries)
        {
            if (!ResourcePath.TryCreate(entry.Path, out var resourcePath))
            {
                error = new ErrorResponse
                {
                    Error = "invalid_resource_metadata",
                    Message = $"Invalid resource metadata path: '{entry.Path}'."
                };
                return false;
            }

            if (entry.UnixMode is { } unixMode && !SkillArchiveBuilder.IsSafeUnixMode(unixMode))
            {
                error = new ErrorResponse
                {
                    Error = "invalid_resource_metadata",
                    Message = $"Invalid unixMode for resource '{entry.Path}'. Must contain only standard permission bits between 0 and 511."
                };
                return false;
            }

            if (!metadata.TryAdd(resourcePath.Value.Value, entry.UnixMode))
            {
                error = new ErrorResponse
                {
                    Error = "invalid_resource_metadata",
                    Message = $"Duplicate resource metadata path: '{entry.Path}'."
                };
                return false;
            }
        }

        return true;
    }

    private static IResult HandleUploadResult(SkillUploadResult result, IConfiguration configuration)
    {
        if (!result.Success)
        {
            if (result.IsDuplicateVersion)
            {
                return Results.Conflict(new ErrorResponse
                {
                    Error = "duplicate_version",
                    Message = result.Error ?? "Version already exists."
                });
            }

            return Results.BadRequest(new ErrorResponse
            {
                Error = "upload_failed",
                Message = result.Error ?? "Upload failed."
            });
        }

        var baseUrl = configuration["SkillServer:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";

        return Results.Created(
            $"/api/v1/skills/{result.Name}/{result.Version}",
            new SkillUploadResponse
            {
                Name = result.Name!.Value.Value,
                Version = result.Version!.Value.Value,
                Sha256 = result.Digest!.Value.Value,
                Url = $"{baseUrl}/api/v1/skills/{result.Name}/{result.Version}/SKILL.md"
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

    private static async Task<IResult> GetLatestVersion(
        string name,
        SkillRepository repository,
        CancellationToken ct)
    {
        var latest = await repository.GetLatestVersionByNameAsync(name, ct);
        if (latest is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Skill '{name}' not found." });

        var summary = new SkillVersionSummary
        {
            Name = name,
            Version = latest.Version,
            Description = latest.Description,
            Category = latest.Category,
            Sha256 = latest.Sha256,
            SizeBytes = latest.SizeBytes,
            PublishedAt = latest.PublishedAt,
            IsLatest = latest.IsLatest,
            FileCount = latest.FileCount
        };

        return Results.Json(summary, SkillServerJsonContext.Default.SkillVersionSummary);
    }

    private static async Task<IResult> CheckUpdates(
        IReadOnlyList<CheckUpdateRequestItem> request,
        SkillRepository repository,
        CancellationToken ct)
    {
        if (request.Count == 0)
            return Results.Json(Array.Empty<CheckUpdateResponseItem>(),
                SkillServerJsonContext.Default.IReadOnlyListCheckUpdateResponseItem);

        if (request.Count > 100)
            return Results.BadRequest(new ErrorResponse
                { Error = "too_many_items", Message = "Maximum 100 items per request." });

        var latestVersions = await repository.CheckUpdatesAsync(
            request.Select(r => (r.Name, r.Version)).ToList(), ct);

        var lookup = latestVersions.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

        var results = request.Select(r =>
        {
            if (!lookup.TryGetValue(r.Name, out var latest))
            {
                return new CheckUpdateResponseItem
                {
                    Name = r.Name,
                    CurrentVersion = r.Version,
                    LatestVersion = r.Version,
                    LatestDigest = "",
                    LatestPublishedAt = DateTimeOffset.MinValue,
                    HasUpdate = false
                };
            }

            return new CheckUpdateResponseItem
            {
                Name = r.Name,
                CurrentVersion = r.Version,
                LatestVersion = latest.LatestVersion,
                LatestDigest = latest.LatestDigest,
                LatestPublishedAt = latest.LatestPublishedAt,
                HasUpdate = !string.Equals(r.Version, latest.LatestVersion, StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();

        return Results.Json(results, SkillServerJsonContext.Default.IReadOnlyListCheckUpdateResponseItem);
    }

    private static async Task<IResult> ListSubAgents(
        SubAgentRepository repository,
        CancellationToken ct)
    {
        var latestVersions = await repository.GetAllLatestVersionsWithMetadataAsync(ct);
        var summaries = latestVersions.Select(v => new SubAgentSummary
        {
            Name = v.SubAgentName,
            Description = v.Description,
            LatestVersion = v.Version,
            VersionCount = v.VersionCount,
            CreatedAt = v.SubAgentCreatedAt,
            UpdatedAt = v.SubAgentUpdatedAt
        }).ToList();

        return Results.Json(summaries, SkillServerJsonContext.Default.IReadOnlyListSubAgentSummary);
    }

    private static async Task<IResult> GetSubAgent(
        string name,
        SubAgentRepository repository,
        CancellationToken ct)
    {
        var subAgent = await repository.GetSubAgentByNameAsync(name, ct);
        if (subAgent is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Sub-agent '{name}' not found." });

        var versions = await repository.GetAllVersionsAsync(subAgent.Id, ct);
        var summaries = versions.Select(v => ToSubAgentVersionSummary(subAgent.Name, v)).ToList();

        return Results.Json(summaries, SkillServerJsonContext.Default.IReadOnlyListSubAgentVersionSummary);
    }

    private static async Task<IResult> GetSubAgentVersion(
        string name,
        string version,
        SubAgentRepository repository,
        CancellationToken ct)
    {
        var subAgent = await repository.GetSubAgentByNameAsync(name, ct);
        if (subAgent is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Sub-agent '{name}' not found." });

        var subAgentVersion = await repository.GetVersionAsync(subAgent.Id, version, ct);
        if (subAgentVersion is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found for sub-agent '{name}'." });

        return Results.Json(ToSubAgentVersionSummary(subAgent.Name, subAgentVersion), SkillServerJsonContext.Default.SubAgentVersionSummary);
    }

    private static async Task<IResult> DownloadSubAgentMd(
        string name,
        string version,
        SubAgentRepository repository,
        BlobStorage blobStorage,
        CancellationToken ct)
    {
        var subAgent = await repository.GetSubAgentByNameAsync(name, ct);
        if (subAgent is null)
            return Results.NotFound();

        var subAgentVersion = await repository.GetVersionAsync(subAgent.Id, version, ct);
        if (subAgentVersion is null)
            return Results.NotFound();

        var stream = blobStorage.GetBlob(subAgentVersion.Sha256);
        if (stream is null)
            return Results.NotFound();

        return Results.File(stream, "text/markdown", "agent.md");
    }

    private static async Task<IResult> UploadSubAgent(
        [FromForm] IFormFile file,
        [FromForm] string name,
        [FromForm] string version,
        SubAgentUploadService uploadService,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!SkillName.TryCreate(name, out var subAgentName))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_name",
                Message = "Invalid sub-agent name. Must be 1-64 lowercase alphanumeric characters and hyphens."
            });
        }

        if (!SkillVersionString.TryCreate(version, out var subAgentVersion))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_version",
                Message = "Invalid version string."
            });
        }

        if (!file.FileName.Equals("agent.md", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_file",
                Message = "File must be agent.md or a markdown file."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await uploadService.UploadSubAgentAsync(subAgentName.Value, subAgentVersion.Value, stream, ct);
        return HandleSubAgentUploadResult(result, configuration);
    }

    private static IResult HandleSubAgentUploadResult(SubAgentUploadResult result, IConfiguration configuration)
    {
        if (!result.Success)
        {
            if (result.IsDuplicateVersion)
            {
                return Results.Conflict(new ErrorResponse
                {
                    Error = "duplicate_version",
                    Message = result.Error ?? "Version already exists."
                });
            }

            return Results.BadRequest(new ErrorResponse
            {
                Error = "upload_failed",
                Message = result.Error ?? "Upload failed."
            });
        }

        var baseUrl = configuration["SkillServer:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";
        return Results.Created(
            $"/api/v1/subagents/{result.Name}/{result.Version}",
            new SubAgentUploadResponse
            {
                Name = result.Name!.Value.Value,
                Version = result.Version!.Value.Value,
                Sha256 = result.Digest!.Value.Value,
                Url = $"{baseUrl}/api/v1/subagents/{result.Name}/{result.Version}/agent.md"
            });
    }

    private static async Task<IResult> DeleteSubAgentVersion(
        string name,
        string version,
        SubAgentRepository repository,
        CancellationToken ct)
    {
        var subAgent = await repository.GetSubAgentByNameAsync(name, ct);
        if (subAgent is null)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Sub-agent '{name}' not found." });

        var deleted = await repository.DeleteVersionAsync(subAgent.Id, version, ct);
        if (!deleted)
            return Results.NotFound(new ErrorResponse { Error = "not_found", Message = $"Version '{version}' not found." });

        return Results.NoContent();
    }

    private static SubAgentVersionSummary ToSubAgentVersionSummary(string name, SubAgentVersion version) => new()
    {
        Name = name,
        Version = version.Version,
        Description = version.Description,
        ModelRole = version.ModelRole,
        TimeoutSeconds = version.TimeoutSeconds,
        PrefillTimeoutSeconds = version.PrefillTimeoutSeconds,
        Visibility = version.Visibility,
        EmitStructuredFindings = version.EmitStructuredFindings,
        Sha256 = version.Sha256,
        SizeBytes = version.SizeBytes,
        PublishedAt = version.PublishedAt,
        IsLatest = version.IsLatest
    };

    private static void MapApiKeyEndpoints(this WebApplication app)
    {
        var keys = app.MapGroup("/api/v1/api-keys")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        keys.MapPost("/", CreateApiKey);
        keys.MapGet("/", ListApiKeys);
        keys.MapDelete("/{id:long}", DeleteApiKey);
    }

    private static async Task<IResult> CreateApiKey(
        CreateApiKeyRequest request,
        ApiKeyService apiKeyService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "invalid_label",
                Message = "API key label is required."
            });
        }

        var (rawKey, storedKey) = await apiKeyService.CreateKeyAsync(
            request.Label, request.ExpiresAt, ct);

        return Results.Created($"/api/v1/api-keys/{storedKey.Id}", new CreateApiKeyResponse
        {
            Id = storedKey.Id,
            Label = storedKey.Label,
            Key = rawKey,
            CreatedAt = storedKey.CreatedAt,
            ExpiresAt = storedKey.ExpiresAt
        });
    }

    private static async Task<IResult> ListApiKeys(
        ApiKeyService apiKeyService,
        CancellationToken ct)
    {
        var keys = await apiKeyService.ListKeysAsync(ct);
        var summaries = keys.Select(k => new ApiKeySummary
        {
            Id = k.Id,
            Label = k.Label,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt
        }).ToList();

        return Results.Json(summaries, SkillServerJsonContext.Default.IReadOnlyListApiKeySummary);
    }

    private static async Task<IResult> DeleteApiKey(
        long id,
        ApiKeyService apiKeyService,
        CancellationToken ct)
    {
        var deleted = await apiKeyService.DeleteKeyAsync(id, ct);
        if (!deleted)
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "delete_failed",
                Message = "Cannot delete key. It may not exist or it may be the last remaining key."
            });
        }

        return Results.NoContent();
    }

    private static void MapBlobEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/blobs/sha256/{digest}", (
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
