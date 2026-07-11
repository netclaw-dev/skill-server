// -----------------------------------------------------------------------
// <copyright file="ListSubAgentsCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Json;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class ListSubAgentsCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help)
        {
            PrintHelp();
            return 0;
        }

        var subAgents = await client.ListSubAgentsAsync();

        if (args.OutputFormat == "json")
        {
            var json = JsonSerializer.Serialize(subAgents, CliJsonContext.Default.IReadOnlyListSubAgentSummary);
            Console.WriteLine(json);
            return 0;
        }

        if (subAgents.Count == 0)
        {
            ConsoleOutput.WriteInfo("No sub-agents found.");
            return 0;
        }

        var headers = new[] { "NAME", "LATEST", "VERSIONS", "UPDATED" };
        var rows = subAgents.Select(s => new[]
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
        Console.WriteLine("Usage: skillserver list-subagents [options]");
        Console.WriteLine();
        Console.WriteLine("List sub-agents on the server.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --output <text|json>  Output format (default: text)");
    }
}
