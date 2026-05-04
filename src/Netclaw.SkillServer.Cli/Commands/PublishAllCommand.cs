// -----------------------------------------------------------------------
// <copyright file="PublishAllCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class PublishAllCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[0];
        var skills = SkillDirectoryScanner.ScanAll(path);

        if (skills.Count == 0)
        {
            ConsoleOutput.WriteWarning($"No skills found in '{path}'.");
            return 0;
        }

        ConsoleOutput.WriteInfo($"Scanning {path}...");
        ConsoleOutput.WriteInfo($"Found {skills.Count} skill(s) to publish.");
        Console.WriteLine();

        var options = new PublishOptions(Force: args.Force, DryRun: args.DryRun, Verbose: args.Verbose);
        var orchestrator = new PublishOrchestrator(client);
        int published = 0, skipped = 0, failed = 0;

        foreach (var skill in skills)
        {
            var result = await orchestrator.PublishAsync(skill, options);

            switch (result.Outcome)
            {
                case PublishOutcome.Published:
                    ConsoleOutput.WriteSuccess($"  {result.Name}@{result.Version}    Published");
                    published++;
                    break;
                case PublishOutcome.Skipped:
                    ConsoleOutput.WriteDim($"  {result.Name}@{result.Version}    Skipped ({result.Message})");
                    skipped++;
                    break;
                case PublishOutcome.Failed:
                    ConsoleOutput.WriteError($"  {result.Name}@{result.Version}    Failed: {result.Message}");
                    failed++;
                    break;
            }
        }

        Console.WriteLine();
        ConsoleOutput.WriteInfo($"Results: {published} published, {skipped} skipped, {failed} failed");

        return failed > 0 ? 2 : 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver publish-all <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Batch-publish all skills found in subdirectories of <path>.");
        Console.WriteLine("Each subdirectory must contain a SKILL.md with name and version in frontmatter.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>                Parent directory containing skill subdirectories");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --force, -f           Delete existing versions before re-publishing");
        Console.WriteLine("  --dry-run             Show what would be published without uploading");
        Console.WriteLine("  --verbose, -v         Show detailed upload progress");
    }
}
