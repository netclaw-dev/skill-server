// -----------------------------------------------------------------------
// <copyright file="PublishSubAgentsCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class PublishSubAgentsCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[0];
        var results = SubAgentFileScanner.ValidateDirectory(path);
        if (results.Count == 0)
        {
            ConsoleOutput.WriteWarning($"No sub-agent markdown files found in '{path}'.");
            return 0;
        }

        ConsoleOutput.WriteInfo($"Scanning {path}...");
        ConsoleOutput.WriteInfo($"Found {results.Count} sub-agent file(s).");
        Console.WriteLine();

        var publishItems = ValidateForPublish(results, args.VersionOverride);
        foreach (var warning in publishItems.Warnings)
            ConsoleOutput.WriteWarning($"  {warning}");

        foreach (var issue in publishItems.Issues)
            ConsoleOutput.WriteError($"  {issue}");

        if (publishItems.Issues.Count > 0)
        {
            Console.WriteLine();
            ConsoleOutput.WriteError($"{publishItems.Issues.Count} error(s), {publishItems.Warnings.Count} warning(s) - publish failed");
            return 1;
        }

        int published = 0, skipped = 0, failed = 0;
        foreach (var item in publishItems.Items)
        {
            if (args.DryRun)
            {
                ConsoleOutput.WriteWarning($"  Dry run: would publish sub-agent {item.SubAgent.Name}@{item.Version} from {item.SubAgent.FilePath}");
                skipped++;
                continue;
            }

            var outcome = await PublishOneAsync(client, item.SubAgent, item.Version, args.Force, args.Verbose);
            switch (outcome.Outcome)
            {
                case BatchPublishOutcome.Published:
                    ConsoleOutput.WriteSuccess($"  {item.SubAgent.Name}@{item.Version}    Published");
                    published++;
                    break;
                case BatchPublishOutcome.Skipped:
                    ConsoleOutput.WriteDim($"  {item.SubAgent.Name}@{item.Version}    Skipped ({outcome.Message})");
                    skipped++;
                    break;
                case BatchPublishOutcome.Failed:
                    ConsoleOutput.WriteError($"  {item.SubAgent.Name}@{item.Version}    Failed: {outcome.Message}");
                    failed++;
                    break;
            }
        }

        Console.WriteLine();
        ConsoleOutput.WriteInfo($"Results: {published} published, {skipped} skipped, {failed} failed");
        return failed > 0 ? 2 : 0;
    }

    internal static BatchSubAgentPublishValidation ValidateForPublish(
        IReadOnlyList<SubAgentLintResult> results,
        string? defaultVersion)
    {
        var issues = new List<string>();
        var warnings = new List<string>();
        var items = new List<BatchSubAgentPublishItem>();

        if (defaultVersion is not null && !SubAgentFileScanner.IsValidVersion(defaultVersion))
            issues.Add($"Invalid --version value '{defaultVersion}'");

        foreach (var result in results)
        {
            issues.AddRange(result.Issues);
            warnings.AddRange(result.Warnings);

            if (result.SubAgent is null)
                continue;

            var version = result.SubAgent.Version ?? defaultVersion;
            if (!SubAgentFileScanner.IsValidVersion(version))
            {
                issues.Add($"{Path.GetFileName(result.SubAgent.FilePath)}: Missing publication version. Add frontmatter 'version' or pass --version <version>.");
                continue;
            }

            items.Add(new BatchSubAgentPublishItem(result.SubAgent, version!));
        }

        foreach (var group in items.GroupBy(i => i.SubAgent.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            issues.Add($"Duplicate sub-agent name '{group.Key}' found in input set: {string.Join(", ", group.Select(i => i.SubAgent.FilePath))}");
        }

        return new BatchSubAgentPublishValidation(items, issues, warnings);
    }

    private static async Task<BatchPublishResult> PublishOneAsync(
        SkillServerClient client,
        ScannedSubAgent subAgent,
        string version,
        bool force,
        bool verbose)
    {
        try
        {
            if (force)
                await DeleteExistingAsync(client, subAgent.Name, version, verbose);

            await using var stream = File.OpenRead(subAgent.FilePath);
            if (verbose)
                ConsoleOutput.WriteDim($"  Uploading {subAgent.FilePath} ({FormatSize(stream.Length)})");

            var response = await client.UploadSubAgentIfNotExistsAsync(subAgent.Name, version, stream);
            return response is null
                ? new BatchPublishResult(BatchPublishOutcome.Skipped, "Already published")
                : new BatchPublishResult(BatchPublishOutcome.Published, response.Sha256);
        }
        catch (HttpRequestException ex)
        {
            return new BatchPublishResult(BatchPublishOutcome.Failed, ex.Message);
        }
        catch (IOException ex)
        {
            return new BatchPublishResult(BatchPublishOutcome.Failed, ex.Message);
        }
    }

    private static async Task DeleteExistingAsync(
        SkillServerClient client,
        string name,
        string version,
        bool verbose)
    {
        try
        {
            await client.DeleteSubAgentVersionAsync(name, version);
            if (verbose)
                ConsoleOutput.WriteDim($"  Deleted existing sub-agent {name}@{version}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            if (verbose)
                ConsoleOutput.WriteDim($"  No existing sub-agent version to delete for {name}@{version}");
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver publish-subagents <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Batch-publish all NetClaw-compatible sub-agent markdown files under <path>.");
        Console.WriteLine("Each file must include frontmatter name and description. Publication version comes from frontmatter version or --version.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>                Directory containing .md sub-agent definitions");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version <version>   Default publication version for files without frontmatter version");
        Console.WriteLine("  --force, -f           Delete existing versions before re-publishing");
        Console.WriteLine("  --dry-run             Validate and show what would be published without uploading");
        Console.WriteLine("  --verbose, -v         Show detailed upload progress");
    }
}

internal sealed record BatchSubAgentPublishItem(ScannedSubAgent SubAgent, string Version);

internal sealed record BatchSubAgentPublishValidation(
    IReadOnlyList<BatchSubAgentPublishItem> Items,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Warnings);

internal sealed record BatchPublishResult(BatchPublishOutcome Outcome, string Message);

internal enum BatchPublishOutcome
{
    Published,
    Skipped,
    Failed
}
