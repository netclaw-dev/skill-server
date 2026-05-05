// -----------------------------------------------------------------------
// <copyright file="ApiKeyCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Json;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class ApiKeyCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help)
        {
            PrintHelp();
            return 0;
        }

        return args.SubCommand switch
        {
            "create" => await Create(args, client),
            "list" => await List(args, client),
            "delete" => await Delete(args, client),
            _ => ShowSubcommandHelp()
        };
    }

    private static async Task<int> Create(ParsedArgs args, SkillServerClient client)
    {
        var label = args.Label;
        if (string.IsNullOrWhiteSpace(label))
        {
            ConsoleOutput.WriteError("Error: --label is required.");
            return 1;
        }

        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(args.ExpiresAt))
        {
            if (DateTimeOffset.TryParse(args.ExpiresAt, out var parsed))
                expiresAt = parsed;
            else
            {
                ConsoleOutput.WriteError($"Error: Invalid date format for --expires-at: '{args.ExpiresAt}'");
                return 1;
            }
        }

        try
        {
            var response = await client.CreateApiKeyAsync(label, expiresAt);
            ConsoleOutput.WriteSuccess($"Created API key: {response.Key}");
            ConsoleOutput.WriteInfo($"Label: {response.Label}");
            ConsoleOutput.WriteInfo($"ID: {response.Id}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            return ConsoleOutput.HandleHttpError(ex);
        }
    }

    private static async Task<int> List(ParsedArgs args, SkillServerClient client)
    {
        try
        {
            var keys = await client.ListApiKeysAsync();

            if (args.OutputFormat == "json")
            {
                var json = JsonSerializer.Serialize(keys,
                    CliJsonContext.Default.IReadOnlyListApiKeySummary);
                Console.WriteLine(json);
                return 0;
            }

            if (keys.Count == 0)
            {
                ConsoleOutput.WriteInfo("No API keys found.");
                return 0;
            }

            var headers = new[] { "ID", "LABEL", "CREATED", "EXPIRES" };
            var rows = keys.Select(k => new[]
            {
                k.Id.ToString(),
                k.Label,
                k.CreatedAt.ToString("yyyy-MM-dd"),
                k.ExpiresAt?.ToString("yyyy-MM-dd") ?? "never"
            }).ToList();

            ConsoleOutput.WriteTable(headers, rows);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            return ConsoleOutput.HandleHttpError(ex);
        }
    }

    private static async Task<int> Delete(ParsedArgs args, SkillServerClient client)
    {
        if (args.Positional.Count == 0)
        {
            ConsoleOutput.WriteError("Usage: skillserver api-key delete <id>");
            return 1;
        }

        if (!long.TryParse(args.Positional[0], out var id))
        {
            ConsoleOutput.WriteError($"Error: Invalid API key ID: '{args.Positional[0]}'");
            return 1;
        }

        try
        {
            await client.DeleteApiKeyAsync(id);
            ConsoleOutput.WriteSuccess($"Deleted API key {id}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            return ConsoleOutput.HandleHttpError(ex);
        }
    }

    private static int ShowSubcommandHelp()
    {
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver api-key <subcommand> [options]");
        Console.WriteLine();
        Console.WriteLine("Manage server API keys.");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  create                Create a new API key");
        Console.WriteLine("  list                  List all API keys");
        Console.WriteLine("  delete <id>           Delete an API key");
        Console.WriteLine();
        Console.WriteLine("Create options:");
        Console.WriteLine("  --label <label>       Key label (required)");
        Console.WriteLine("  --expires-at <date>   Expiration date (optional)");
    }
}
