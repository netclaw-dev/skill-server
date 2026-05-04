// -----------------------------------------------------------------------
// <copyright file="ConfigResolverTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Config;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class ConfigResolverTests
{
    [Fact]
    public void Resolve_CliFlags_TakePriority()
    {
        var resolver = new ConfigResolver();
        var result = resolver.Resolve(
            flagServerUrl: "https://flag.example.com",
            flagApiKey: "flag-key");

        Assert.Equal("https://flag.example.com", result.ServerUrl);
        Assert.Equal("flag-key", result.ApiKey);
        Assert.True(result.HasServerUrl);
        Assert.True(result.HasApiKey);
    }

    [Fact]
    public void Resolve_NoConfig_ReturnsEmpty()
    {
        var originalUrl = Environment.GetEnvironmentVariable("SKILLSERVER_URL");
        var originalKey = Environment.GetEnvironmentVariable("SKILLSERVER_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", null);
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", null);

            var resolver = new ConfigResolver();
            var result = resolver.Resolve();

            Assert.False(result.HasServerUrl);
            Assert.False(result.HasApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", originalUrl);
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", originalKey);
        }
    }

    [Fact]
    public void Resolve_EnvVars_UsedWhenNoFlags()
    {
        var originalUrl = Environment.GetEnvironmentVariable("SKILLSERVER_URL");
        var originalKey = Environment.GetEnvironmentVariable("SKILLSERVER_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", "https://env.example.com");
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", "env-key");

            var resolver = new ConfigResolver();
            var result = resolver.Resolve();

            Assert.Equal("https://env.example.com", result.ServerUrl);
            Assert.Equal("env-key", result.ApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", originalUrl);
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", originalKey);
        }
    }

    [Fact]
    public void Resolve_Flags_OverrideEnvVars()
    {
        var originalUrl = Environment.GetEnvironmentVariable("SKILLSERVER_URL");
        var originalKey = Environment.GetEnvironmentVariable("SKILLSERVER_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", "https://env.example.com");
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", "env-key");

            var resolver = new ConfigResolver();
            var result = resolver.Resolve(
                flagServerUrl: "https://flag.example.com",
                flagApiKey: "flag-key");

            Assert.Equal("https://flag.example.com", result.ServerUrl);
            Assert.Equal("flag-key", result.ApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SKILLSERVER_URL", originalUrl);
            Environment.SetEnvironmentVariable("SKILLSERVER_API_KEY", originalKey);
        }
    }
}
