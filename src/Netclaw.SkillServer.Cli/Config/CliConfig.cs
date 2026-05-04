// -----------------------------------------------------------------------
// <copyright file="CliConfig.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Netclaw.SkillServer.Cli.Config;

internal sealed class CliConfig
{
    [JsonPropertyName("configVersion")]
    public int ConfigVersion { get; set; } = 1;

    [JsonPropertyName("serverUrl")]
    public string? ServerUrl { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}
