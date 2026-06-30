// -----------------------------------------------------------------------
// <copyright file="PublishSubAgentCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class PublishSubAgentCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        if (!SubAgentFileScanner.IsValidVersion(args.VersionOverride))
        {
            ConsoleOutput.WriteError("Error: --version <version> is required for sub-agent publication.");
            return 1;
        }

        var result = SubAgentFileScanner.ValidateFile(args.Positional[0]);
        foreach (var warning in result.Warnings)
            ConsoleOutput.WriteWarning($"Warning: {warning}");

        if (result.Issues.Count > 0)
        {
            foreach (var issue in result.Issues)
                ConsoleOutput.WriteError($"Error: {issue}");
            return 1;
        }

        var subAgent = result.SubAgent!;
        var version = args.VersionOverride!;

        if (args.DryRun)
        {
            ConsoleOutput.WriteWarning(
                $"Dry run: would publish sub-agent {subAgent.Name}@{version} from {subAgent.FilePath}");
            return 0;
        }

        ConsoleOutput.WriteInfo($"Publishing sub-agent {subAgent.Name}@{version}...");

        if (args.Force)
        {
            await DeleteExistingAsync(client, subAgent.Name, version, args.Verbose);
        }

        try
        {
            await using var stream = File.OpenRead(subAgent.FilePath);
            if (args.Verbose)
                ConsoleOutput.WriteDim($"  Uploading agent.md ({FormatSize(stream.Length)})");

            var response = await client.UploadSubAgentIfNotExistsAsync(subAgent.Name, version, stream);
            if (response is null)
            {
                ConsoleOutput.WriteWarning($"Skipped sub-agent {subAgent.Name}@{version} (Already published)");
                return 0;
            }

            ConsoleOutput.WriteSuccess($"Published sub-agent {response.Name}@{response.Version}");
            ConsoleOutput.WriteDim($"  {response.Sha256}");
            ConsoleOutput.WriteDim($"  {response.Url}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            return ConsoleOutput.HandleHttpError(ex);
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
        Console.WriteLine("Usage: skillserver publish-subagent <path> --version <version> [options]");
        Console.WriteLine();
        Console.WriteLine("Publish a single NetClaw-compatible sub-agent markdown file to the server.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>                Path to agent.md or another .md sub-agent definition");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version <version>   SkillServer publication version (required)");
        Console.WriteLine("  --force, -f           Delete existing version before re-publishing");
        Console.WriteLine("  --dry-run             Validate and show what would be published without uploading");
        Console.WriteLine("  --verbose, -v         Show detailed upload progress");
    }
}
