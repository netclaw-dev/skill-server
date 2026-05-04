// -----------------------------------------------------------------------
// <copyright file="ConfigCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Config;
using Netclaw.SkillServer.Cli.Output;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class ConfigCommand
{
    public static Task<int> ExecuteAsync(ParsedArgs args)
    {
        if (args.Help)
        {
            PrintHelp();
            return Task.FromResult(0);
        }

        return args.SubCommand switch
        {
            "init" => Task.FromResult(Init()),
            "set" => Task.FromResult(Set(args)),
            "show" => Task.FromResult(Show()),
            _ => Task.FromResult(ShowSubcommandHelp())
        };
    }

    private static int Init()
    {
        Console.Write("SkillServer URL: ");
        var serverUrl = Console.ReadLine()?.Trim();

        Console.Write("API Key: ");
        var apiKey = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            ConsoleOutput.WriteError("Error: Server URL is required.");
            return 1;
        }

        var config = new CliConfig
        {
            ServerUrl = serverUrl,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey
        };

        ConfigResolver.SaveConfigFile(config);
        ConsoleOutput.WriteSuccess($"Configuration saved to {ConfigResolver.ConfigFilePath}");
        return 0;
    }

    private static int Set(ParsedArgs args)
    {
        if (args.Key is null || args.Value is null)
        {
            ConsoleOutput.WriteError("Usage: skillserver config set <key> <value>");
            ConsoleOutput.WriteError("Keys: server-url, api-key");
            return 1;
        }

        var config = ConfigResolver.LoadConfigFile() ?? new CliConfig();

        switch (args.Key.ToLowerInvariant())
        {
            case "server-url":
                config.ServerUrl = args.Value;
                break;
            case "api-key":
                config.ApiKey = args.Value;
                break;
            default:
                ConsoleOutput.WriteError($"Unknown config key: '{args.Key}'. Valid keys: server-url, api-key");
                return 1;
        }

        ConfigResolver.SaveConfigFile(config);
        ConsoleOutput.WriteSuccess($"Set {args.Key} in {ConfigResolver.ConfigFilePath}");
        return 0;
    }

    private static int Show()
    {
        var config = ConfigResolver.LoadConfigFile();

        if (config is null)
        {
            ConsoleOutput.WriteInfo("No configuration file found.");
            ConsoleOutput.WriteInfo($"Run 'skillserver config init' to create one at {ConfigResolver.ConfigFilePath}");
            return 0;
        }

        ConsoleOutput.WriteInfo($"server-url: {config.ServerUrl ?? "(not set)"}");
        ConsoleOutput.WriteInfo($"api-key:    {(config.ApiKey is not null ? ConsoleOutput.MaskApiKey(config.ApiKey) : "(not set)")}");
        ConsoleOutput.WriteDim($"source:     {ConfigResolver.ConfigFilePath}");

        // Show env var overrides if present
        var envUrl = Environment.GetEnvironmentVariable("SKILLSERVER_URL");
        var envKey = Environment.GetEnvironmentVariable("SKILLSERVER_API_KEY");
        if (envUrl is not null)
            ConsoleOutput.WriteWarning($"SKILLSERVER_URL env var set (overrides config): {envUrl}");
        if (envKey is not null)
            ConsoleOutput.WriteWarning($"SKILLSERVER_API_KEY env var set (overrides config)");

        return 0;
    }

    private static int ShowSubcommandHelp()
    {
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver config <subcommand>");
        Console.WriteLine();
        Console.WriteLine("Manage CLI configuration.");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  init                  Interactive first-run setup");
        Console.WriteLine("  set <key> <value>     Set a configuration value");
        Console.WriteLine("  show                  Show current configuration");
        Console.WriteLine();
        Console.WriteLine("Keys: server-url, api-key");
    }
}
