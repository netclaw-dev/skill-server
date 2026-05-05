// -----------------------------------------------------------------------
// <copyright file="CliArgsParser.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Netclaw.SkillServer.Cli;

internal sealed class ParsedArgs
{
    public string Command { get; init; } = "";
    public string SubCommand { get; init; } = "";
    public IReadOnlyList<string> Positional { get; init; } = [];
    public string? ServerUrl { get; init; }
    public string? ApiKey { get; init; }
    public string? OutputFormat { get; init; }
    public bool Verbose { get; init; }
    public bool Help { get; init; }
    public bool Version { get; init; }
    public bool Force { get; init; }
    public bool DryRun { get; init; }
    public bool Yes { get; init; }
    public string? VersionOverride { get; init; }
    public string? Search { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? Label { get; init; }
    public string? ExpiresAt { get; init; }
    public string? Key { get; init; }
    public string? Value { get; init; }
}

internal static class CliArgsParser
{
    public static ParsedArgs Parse(string[] args)
    {
        var positional = new List<string>();
        string? serverUrl = null, apiKey = null, outputFormat = null;
        string? versionOverride = null, search = null, label = null, expiresAt = null;
        string? configKey = null, configValue = null;
        int? skip = null, take = null;
        bool verbose = false, help = false, version = false, force = false, dryRun = false, yes = false;

        var command = "";
        var subCommand = "";
        var commandParsed = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (!commandParsed && !arg.StartsWith('-'))
            {
                if (command == "")
                {
                    command = arg.ToLowerInvariant();

                    if (command is "api-key" or "config" or "publish-all")
                    {
                        if (command is "api-key" or "config" && i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        {
                            subCommand = args[++i].ToLowerInvariant();
                        }

                        commandParsed = true;
                    }
                    else
                    {
                        commandParsed = true;
                    }

                    continue;
                }
            }

            switch (arg)
            {
                case "--server-url" when i + 1 < args.Length:
                    serverUrl = args[++i];
                    break;
                case "--api-key" when i + 1 < args.Length:
                    apiKey = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    outputFormat = args[++i].ToLowerInvariant();
                    break;
                case "--version" when command == "":
                    version = true;
                    break;
                case "--version" when i + 1 < args.Length:
                    versionOverride = args[++i];
                    break;
                case "--search" when i + 1 < args.Length:
                    search = args[++i];
                    break;
                case "--skip" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var s)) skip = s;
                    break;
                case "--take" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var t)) take = t;
                    break;
                case "--label" when i + 1 < args.Length:
                    label = args[++i];
                    break;
                case "--expires-at" when i + 1 < args.Length:
                    expiresAt = args[++i];
                    break;
                case "--verbose" or "-v":
                    verbose = true;
                    break;
                case "--help" or "-h":
                    help = true;
                    break;
                case "--force" or "-f":
                    force = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--yes" or "-y":
                    yes = true;
                    break;
                case "-V":
                    version = true;
                    break;
                default:
                    if (!arg.StartsWith('-'))
                    {
                        if (command == "config" && subCommand == "set")
                        {
                            if (configKey is null)
                                configKey = arg;
                            else
                                configValue ??= arg;
                        }
                        else
                        {
                            positional.Add(arg);
                        }
                    }
                    break;
            }
        }

        return new ParsedArgs
        {
            Command = command,
            SubCommand = subCommand,
            Positional = positional,
            ServerUrl = serverUrl,
            ApiKey = apiKey,
            OutputFormat = outputFormat,
            Verbose = verbose,
            Help = help,
            Version = version,
            Force = force,
            DryRun = dryRun,
            Yes = yes,
            VersionOverride = versionOverride,
            Search = search,
            Skip = skip,
            Take = take,
            Label = label,
            ExpiresAt = expiresAt,
            Key = configKey,
            Value = configValue
        };
    }
}
