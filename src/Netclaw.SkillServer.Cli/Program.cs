// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli;
using Netclaw.SkillServer.Cli.Commands;
using Netclaw.SkillServer.Cli.Config;
using Netclaw.SkillServer.Cli.Output;

var parsedArgs = CliArgsParser.Parse(args);

if (parsedArgs.Version)
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "unknown";
    Console.WriteLine($"skillserver {version}");
    return 0;
}

if (parsedArgs.Help && parsedArgs.Command == "")
{
    PrintHelp();
    return 0;
}

if (parsedArgs.Command == "")
{
    PrintHelp();
    return 1;
}

// Config command doesn't need a server connection
if (parsedArgs.Command == "config")
    return await ConfigCommand.ExecuteAsync(parsedArgs);

// --help on subcommands should work without auth
if (parsedArgs.Help)
{
    using var helpClient = new SkillServerClient("http://placeholder");
    return parsedArgs.Command switch
    {
        "publish" => await PublishCommand.ExecuteAsync(parsedArgs, helpClient),
        "publish-all" => await PublishAllCommand.ExecuteAsync(parsedArgs, helpClient),
        "delete" => await DeleteCommand.ExecuteAsync(parsedArgs, helpClient),
        "list" => await ListCommand.ExecuteAsync(parsedArgs, helpClient),
        "versions" => await VersionsCommand.ExecuteAsync(parsedArgs, helpClient),
        "verify" => await VerifyCommand.ExecuteAsync(parsedArgs, helpClient),
        "api-key" => await ApiKeyCommand.ExecuteAsync(parsedArgs, helpClient),
        _ => UnknownCommand(parsedArgs.Command)
    };
}

// All other commands need resolved config
var resolver = new ConfigResolver();
var config = resolver.Resolve(parsedArgs.ServerUrl, parsedArgs.ApiKey);

// Read-only commands that don't require auth
var readOnlyCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "list", "versions", "verify" };

var requiresAuth = !readOnlyCommands.Contains(parsedArgs.Command);

if (!config.HasServerUrl)
{
    ConsoleOutput.WriteError("Error: Server URL not configured.");
    ConsoleOutput.WriteError("Set SKILLSERVER_URL or run 'skillserver config init'");
    return 1;
}

if (requiresAuth && !config.HasApiKey)
{
    ConsoleOutput.WriteError("Error: Authentication required.");
    ConsoleOutput.WriteError("Set SKILLSERVER_API_KEY or run 'skillserver config init'");
    return 1;
}

using var client = new SkillServerClient(config.ServerUrl!, config.ApiKey);

return parsedArgs.Command switch
{
    "publish" => await PublishCommand.ExecuteAsync(parsedArgs, client),
    "publish-all" => await PublishAllCommand.ExecuteAsync(parsedArgs, client),
    "delete" => await DeleteCommand.ExecuteAsync(parsedArgs, client),
    "list" => await ListCommand.ExecuteAsync(parsedArgs, client),
    "versions" => await VersionsCommand.ExecuteAsync(parsedArgs, client),
    "verify" => await VerifyCommand.ExecuteAsync(parsedArgs, client),
    "api-key" => await ApiKeyCommand.ExecuteAsync(parsedArgs, client),
    _ => UnknownCommand(parsedArgs.Command)
};

static int UnknownCommand(string command)
{
    ConsoleOutput.WriteError($"Unknown command: '{command}'");
    Console.WriteLine();
    PrintHelp();
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("Usage: skillserver <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  publish <path>            Publish a skill directory to the server");
    Console.WriteLine("  publish-all <path>        Batch-publish all skills in a directory");
    Console.WriteLine("  delete <name> <version>   Delete a published skill version");
    Console.WriteLine("  list                      List skills on the server");
    Console.WriteLine("  versions <name>           List all versions of a skill");
    Console.WriteLine("  verify <path>             Verify local skill matches published version");
    Console.WriteLine("  config                    Manage CLI configuration");
    Console.WriteLine("  api-key                   Manage server API keys");
    Console.WriteLine();
    Console.WriteLine("Global options:");
    Console.WriteLine("  --server-url <url>        SkillServer URL (overrides config/env)");
    Console.WriteLine("  --api-key <key>           API key (overrides config/env)");
    Console.WriteLine("  --output <text|json>      Output format (default: text)");
    Console.WriteLine("  --verbose, -v             Enable verbose output");
    Console.WriteLine("  --help, -h                Show help");
    Console.WriteLine("  --version                 Show version");
}
