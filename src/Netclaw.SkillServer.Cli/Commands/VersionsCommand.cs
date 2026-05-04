// -----------------------------------------------------------------------
// <copyright file="VersionsCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Json;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class VersionsCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var name = args.Positional[0];
        var versions = await client.GetSkillVersionsAsync(name);

        if (args.OutputFormat == "json")
        {
            var json = JsonSerializer.Serialize(versions,
                CliJsonContext.Default.IReadOnlyListSkillVersionSummary);
            Console.WriteLine(json);
            return 0;
        }

        if (versions.Count == 0)
        {
            ConsoleOutput.WriteInfo($"No versions found for '{name}'.");
            return 0;
        }

        var headers = new[] { "VERSION", "PUBLISHED", "LATEST", "SHA256" };
        var rows = versions.Select(v => new[]
        {
            v.Version,
            v.PublishedAt.ToString("yyyy-MM-dd"),
            v.IsLatest ? "*" : "",
            v.Sha256.Length > 15 ? v.Sha256[..15] + "..." : v.Sha256
        }).ToList();

        ConsoleOutput.WriteTable(headers, rows);
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver versions <name> [options]");
        Console.WriteLine();
        Console.WriteLine("List all versions of a skill.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <name>                Skill name");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --output <text|json>  Output format (default: text)");
    }
}
