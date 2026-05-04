// -----------------------------------------------------------------------
// <copyright file="ListCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Json;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class ListCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help)
        {
            PrintHelp();
            return 0;
        }

        IReadOnlyList<SkillSummary> skills;

        if (!string.IsNullOrWhiteSpace(args.Search))
            skills = await client.SearchSkillsAsync(args.Search, args.Skip, args.Take);
        else
            skills = await client.ListSkillsAsync(args.Skip, args.Take);

        if (args.OutputFormat == "json")
        {
            var json = JsonSerializer.Serialize(skills, CliJsonContext.Default.IReadOnlyListSkillSummary);
            Console.WriteLine(json);
            return 0;
        }

        if (skills.Count == 0)
        {
            ConsoleOutput.WriteInfo("No skills found.");
            return 0;
        }

        var headers = new[] { "NAME", "LATEST", "VERSIONS", "UPDATED" };
        var rows = skills.Select(s => new[]
        {
            s.Name,
            s.LatestVersion,
            s.VersionCount.ToString(),
            s.UpdatedAt.ToString("yyyy-MM-dd")
        }).ToList();

        ConsoleOutput.WriteTable(headers, rows);
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver list [options]");
        Console.WriteLine();
        Console.WriteLine("List skills on the server.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --search <query>      Search skills by name or description");
        Console.WriteLine("  --skip <n>            Skip first n results");
        Console.WriteLine("  --take <n>            Limit to n results");
        Console.WriteLine("  --output <text|json>  Output format (default: text)");
    }
}
