// -----------------------------------------------------------------------
// <copyright file="PublishOrchestrator.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Publishing;

public enum PublishOutcome
{
    Published,
    Skipped,
    Failed
}

public sealed record PublishResult(
    string Name,
    string Version,
    PublishOutcome Outcome,
    string? Message = null,
    SkillUploadResponse? Response = null);

public sealed class PublishOrchestrator
{
    private readonly SkillServerClient _client;

    public PublishOrchestrator(SkillServerClient client)
    {
        _client = client;
    }

    public async Task<PublishResult> PublishAsync(
        ScannedSkill skill,
        string? versionOverride = null,
        bool force = false,
        bool dryRun = false,
        bool verbose = false,
        CancellationToken ct = default)
    {
        var version = versionOverride ?? skill.Version;

        if (dryRun)
        {
            var resourceInfo = skill.Resources.Count > 0
                ? $"SKILL.md + {skill.Resources.Count} resources"
                : "SKILL.md only";
            return new PublishResult(skill.Name, version, PublishOutcome.Skipped,
                $"Would publish ({resourceInfo})");
        }

        if (force)
        {
            try
            {
                await _client.DeleteVersionAsync(skill.Name, version, ct);
                if (verbose)
                    ConsoleOutput.WriteDim($"  Deleted existing {skill.Name}@{version}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (verbose)
                    ConsoleOutput.WriteDim($"  No existing version to delete for {skill.Name}@{version}");
            }
        }

        var resources = new List<(string RelativePath, Stream Content)>();
        FileStream? skillMdStream = null;
        try
        {
            skillMdStream = File.OpenRead(skill.SkillMdPath);

            foreach (var resource in skill.Resources)
            {
                resources.Add((resource.RelativePath, File.OpenRead(resource.AbsolutePath)));
            }

            if (verbose)
            {
                var fileSize = new FileInfo(skill.SkillMdPath).Length;
                ConsoleOutput.WriteDim($"  Uploading SKILL.md ({FormatSize(fileSize)})");
                foreach (var resource in skill.Resources)
                {
                    var size = new FileInfo(resource.AbsolutePath).Length;
                    ConsoleOutput.WriteDim($"  Uploading {resource.RelativePath} ({FormatSize(size)})");
                }
            }

            var response = await _client.TryUploadSkillWithResourcesAsync(
                skill.Name, version, skillMdStream, resources, skill.Category, ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return new PublishResult(skill.Name, version, PublishOutcome.Skipped,
                    "Already published");
            }

            response.EnsureSuccessStatusCode();

            var uploadResponse = await response.Content.ReadFromJsonAsync(
                SkillServerClientJsonContext.Default.SkillUploadResponse, ct);

            return new PublishResult(skill.Name, version, PublishOutcome.Published,
                Response: uploadResponse);
        }
        catch (HttpRequestException ex)
        {
            return new PublishResult(skill.Name, version, PublishOutcome.Failed,
                ex.Message);
        }
        finally
        {
            if (skillMdStream is not null)
                await skillMdStream.DisposeAsync();
            foreach (var (_, stream) in resources)
                await stream.DisposeAsync();
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
