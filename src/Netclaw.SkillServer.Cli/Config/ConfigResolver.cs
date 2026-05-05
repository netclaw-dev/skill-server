// -----------------------------------------------------------------------
// <copyright file="ConfigResolver.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text.Json;
using Netclaw.SkillServer.Cli.Json;

namespace Netclaw.SkillServer.Cli.Config;

internal sealed class ConfigResolver
{
    public static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".skillserver");

    public static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    public ResolvedConfig Resolve(string? flagServerUrl = null, string? flagApiKey = null)
    {
        // Priority: CLI flags > env vars > config file
        var fileConfig = LoadConfigFile();

        var serverUrl = flagServerUrl
                        ?? Environment.GetEnvironmentVariable("SKILLSERVER_URL")
                        ?? fileConfig?.ServerUrl;

        var apiKey = flagApiKey
                     ?? Environment.GetEnvironmentVariable("SKILLSERVER_API_KEY")
                     ?? fileConfig?.ApiKey;

        return new ResolvedConfig(serverUrl, apiKey);
    }

    public static CliConfig? LoadConfigFile()
    {
        if (!File.Exists(ConfigFilePath))
            return null;

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.CliConfig);
    }

    public static void SaveConfigFile(CliConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, CliJsonContext.Default.CliConfig);
        File.WriteAllText(ConfigFilePath, json);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(ConfigFilePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}

internal readonly record struct ResolvedConfig(string? ServerUrl, string? ApiKey)
{
    public bool HasServerUrl => !string.IsNullOrWhiteSpace(ServerUrl);
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
