// -----------------------------------------------------------------------
// <copyright file="PublishOrchestrator.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Publishing;

internal enum PublishOutcome
{
    Published,
    Skipped,
    Failed
}

internal readonly record struct PublishOptions(
    string? VersionOverride = null,
    bool Force = false,
    bool DryRun = false,
    bool Verbose = false);

internal sealed record PublishResult(
    string Name,
    string Version,
    PublishOutcome Outcome,
    string? Message = null,
    SkillUploadResponse? Response = null);

internal sealed class PublishOrchestrator
{
    private readonly SkillServerClient _client;

    public PublishOrchestrator(SkillServerClient client)
    {
        _client = client;
    }

    public async Task<PublishResult> PublishAsync(
        ScannedSkill skill,
        PublishOptions options,
        CancellationToken ct = default)
    {
        var version = options.VersionOverride ?? skill.Version;

        if (options.DryRun)
        {
            return new PublishResult(skill.Name, version, PublishOutcome.Skipped,
                $"Would publish ({FormatResourceInfo(skill)})");
        }

        if (options.Force)
        {
            try
            {
                await _client.DeleteVersionAsync(skill.Name, version, ct);
                if (options.Verbose)
                    ConsoleOutput.WriteDim($"  Deleted existing {skill.Name}@{version}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (options.Verbose)
                    ConsoleOutput.WriteDim($"  No existing version to delete for {skill.Name}@{version}");
            }
        }

        var resources = new List<SkillResourceUpload>();
        FileStream? skillMdStream = null;
        try
        {
            skillMdStream = File.OpenRead(skill.SkillMdPath);

            foreach (var resource in skill.Resources)
            {
                resources.Add(new SkillResourceUpload(
                    resource.RelativePath,
                    File.OpenRead(resource.AbsolutePath),
                    resource.UnixMode));
            }

            if (options.Verbose)
            {
                ConsoleOutput.WriteDim($"  Uploading SKILL.md ({FormatSize(skillMdStream.Length)})");
                foreach (var resource in resources)
                    ConsoleOutput.WriteDim($"  Uploading {resource.RelativePath} ({FormatSize(resource.Content.Length)})");
            }

            var uploadResponse = await _client.UploadSkillIfNotExistsWithResourceUploadsAsync(
                skill.Name, version, skillMdStream, resources, skill.Category, ct);

            if (uploadResponse is null)
            {
                return new PublishResult(skill.Name, version, PublishOutcome.Skipped,
                    "Already published");
            }

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
            foreach (var resource in resources)
                await resource.Content.DisposeAsync();
        }
    }

    internal static string FormatResourceInfo(ScannedSkill skill) =>
        skill.Resources.Count > 0
            ? $"SKILL.md + {skill.Resources.Count} resources"
            : "SKILL.md only";

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
