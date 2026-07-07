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
using System.Net.Http.Headers;
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

if (parsedArgs.Command == "config")
    return await ConfigCommand.ExecuteAsync(parsedArgs);

// lint operates entirely on local files — no server URL or auth needed
if (parsedArgs.Command == "lint")
    return await LintCommand.ExecuteAsync(parsedArgs);

// --help on subcommands works without auth
if (parsedArgs.Help)
{
    using var helpClient = new SkillServerClient("http://placeholder");
    return await DispatchAsync(parsedArgs, helpClient);
}

var resolver = new ConfigResolver();
var config = resolver.Resolve(parsedArgs.ServerUrl, parsedArgs.ApiKey);

var requiresAuth = parsedArgs.Command is not "list" and not "versions" and not "verify" and not "download-subagent";

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

using var httpClient = CreateHttpClient(config, parsedArgs.Verbose);
using var client = new SkillServerClient(httpClient);
return await DispatchAsync(parsedArgs, client);

static async Task<int> DispatchAsync(ParsedArgs parsedArgs, SkillServerClient client) =>
    parsedArgs.Command switch
    {
        "publish" => await PublishCommand.ExecuteAsync(parsedArgs, client),
        "publish-subagent" => await PublishSubAgentCommand.ExecuteAsync(parsedArgs, client),
        "publish-subagents" => await PublishSubAgentsCommand.ExecuteAsync(parsedArgs, client),
        "download-subagent" => await DownloadSubAgentCommand.ExecuteAsync(parsedArgs, client),
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

static HttpClient CreateHttpClient(ResolvedConfig config, bool verbose)
{
    HttpMessageHandler handler = verbose
        ? new VerboseLoggingHandler()
        : new HttpClientHandler();

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(config.ServerUrl!.TrimEnd('/') + "/")
    };

    if (!string.IsNullOrEmpty(config.ApiKey))
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.ApiKey);

    return client;
}

static void PrintHelp()
{
    Console.WriteLine("Usage: skillserver <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  publish <path>            Publish a skill directory to the server");
    Console.WriteLine("  publish-subagent <path>   Publish a sub-agent markdown file to the server");
    Console.WriteLine("  publish-subagents <path>  Batch-publish sub-agent markdown files");
    Console.WriteLine("  download-subagent <n> <v> <path> Download a verified sub-agent artifact");
    Console.WriteLine("  publish-all <path>        Batch-publish all skills in a directory");
    Console.WriteLine("  lint <path>               Validate skills or sub-agents (no auth required)");
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
