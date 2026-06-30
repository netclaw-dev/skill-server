// -----------------------------------------------------------------------
// <copyright file="CliArgsParserTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class CliArgsParserTests
{
    [Fact]
    public void Parse_VersionFlag_SetsVersion()
    {
        var result = CliArgsParser.Parse(["--version"]);

        Assert.True(result.Version);
        Assert.Equal("", result.Command);
    }

    [Fact]
    public void Parse_HelpFlag_SetsHelp()
    {
        var result = CliArgsParser.Parse(["--help"]);

        Assert.True(result.Help);
    }

    [Fact]
    public void Parse_ShortHelpFlag_SetsHelp()
    {
        var result = CliArgsParser.Parse(["-h"]);

        Assert.True(result.Help);
    }

    [Fact]
    public void Parse_PublishCommand_WithPath()
    {
        var result = CliArgsParser.Parse(["publish", "./my-skill"]);

        Assert.Equal("publish", result.Command);
        Assert.Single(result.Positional);
        Assert.Equal("./my-skill", result.Positional[0]);
    }

    [Fact]
    public void Parse_PublishCommand_WithAllFlags()
    {
        var result = CliArgsParser.Parse([
            "publish", "./my-skill",
            "--version", "2.0.0",
            "--force",
            "--dry-run",
            "--verbose",
            "--server-url", "https://example.com",
            "--api-key", "sk-test"
        ]);

        Assert.Equal("publish", result.Command);
        Assert.Equal("./my-skill", result.Positional[0]);
        Assert.Equal("2.0.0", result.VersionOverride);
        Assert.True(result.Force);
        Assert.True(result.DryRun);
        Assert.True(result.Verbose);
        Assert.Equal("https://example.com", result.ServerUrl);
        Assert.Equal("sk-test", result.ApiKey);
    }

    [Fact]
    public void Parse_PublishAllCommand()
    {
        var result = CliArgsParser.Parse(["publish-all", "./skills"]);

        Assert.Equal("publish-all", result.Command);
        Assert.Single(result.Positional);
        Assert.Equal("./skills", result.Positional[0]);
    }

    [Fact]
    public void Parse_PublishSubAgentCommand_WithAllFlags()
    {
        var result = CliArgsParser.Parse([
            "publish-subagent", "./agents/support-agent.md",
            "--version", "1.0.0",
            "--force",
            "--dry-run",
            "--verbose"
        ]);

        Assert.Equal("publish-subagent", result.Command);
        Assert.Equal("./agents/support-agent.md", result.Positional[0]);
        Assert.Equal("1.0.0", result.VersionOverride);
        Assert.True(result.Force);
        Assert.True(result.DryRun);
        Assert.True(result.Verbose);
    }

    [Fact]
    public void Parse_DownloadSubAgentCommand_WithFlags()
    {
        var result = CliArgsParser.Parse([
            "download-subagent", "support-agent", "1.0.0", "./staging/support-agent.md",
            "--force",
            "--dry-run",
            "--verbose"
        ]);

        Assert.Equal("download-subagent", result.Command);
        Assert.Equal("support-agent", result.Positional[0]);
        Assert.Equal("1.0.0", result.Positional[1]);
        Assert.Equal("./staging/support-agent.md", result.Positional[2]);
        Assert.True(result.Force);
        Assert.True(result.DryRun);
        Assert.True(result.Verbose);
    }

    [Fact]
    public void Parse_ListCommand_WithSearch()
    {
        var result = CliArgsParser.Parse(["list", "--search", "akka", "--take", "10"]);

        Assert.Equal("list", result.Command);
        Assert.Equal("akka", result.Search);
        Assert.Equal(10, result.Take);
    }

    [Fact]
    public void Parse_ListCommand_WithJsonOutput()
    {
        var result = CliArgsParser.Parse(["list", "--output", "json"]);

        Assert.Equal("list", result.Command);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public void Parse_DeleteCommand_WithArgs()
    {
        var result = CliArgsParser.Parse(["delete", "my-skill", "1.0.0", "--yes"]);

        Assert.Equal("delete", result.Command);
        Assert.Equal(2, result.Positional.Count);
        Assert.Equal("my-skill", result.Positional[0]);
        Assert.Equal("1.0.0", result.Positional[1]);
        Assert.True(result.Yes);
    }

    [Fact]
    public void Parse_VersionsCommand()
    {
        var result = CliArgsParser.Parse(["versions", "my-skill"]);

        Assert.Equal("versions", result.Command);
        Assert.Single(result.Positional);
        Assert.Equal("my-skill", result.Positional[0]);
    }

    [Fact]
    public void Parse_ConfigInit()
    {
        var result = CliArgsParser.Parse(["config", "init"]);

        Assert.Equal("config", result.Command);
        Assert.Equal("init", result.SubCommand);
    }

    [Fact]
    public void Parse_ConfigSet()
    {
        var result = CliArgsParser.Parse(["config", "set", "server-url", "https://example.com"]);

        Assert.Equal("config", result.Command);
        Assert.Equal("set", result.SubCommand);
        Assert.Equal("server-url", result.Key);
        Assert.Equal("https://example.com", result.Value);
    }

    [Fact]
    public void Parse_ApiKeyCreate()
    {
        var result = CliArgsParser.Parse(["api-key", "create", "--label", "CI Pipeline"]);

        Assert.Equal("api-key", result.Command);
        Assert.Equal("create", result.SubCommand);
        Assert.Equal("CI Pipeline", result.Label);
    }

    [Fact]
    public void Parse_ApiKeyDelete()
    {
        var result = CliArgsParser.Parse(["api-key", "delete", "7"]);

        Assert.Equal("api-key", result.Command);
        Assert.Equal("delete", result.SubCommand);
        Assert.Single(result.Positional);
        Assert.Equal("7", result.Positional[0]);
    }

    [Fact]
    public void Parse_ShortFlags()
    {
        var result = CliArgsParser.Parse(["publish", "./skill", "-f", "-v"]);

        Assert.True(result.Force);
        Assert.True(result.Verbose);
    }

    [Fact]
    public void Parse_EmptyArgs_NoCommand()
    {
        var result = CliArgsParser.Parse([]);

        Assert.Equal("", result.Command);
        Assert.False(result.Help);
        Assert.False(result.Version);
    }

    [Fact]
    public void Parse_SkipAndTake()
    {
        var result = CliArgsParser.Parse(["list", "--skip", "20", "--take", "50"]);

        Assert.Equal(20, result.Skip);
        Assert.Equal(50, result.Take);
    }

    [Fact]
    public void Parse_VerifyCommand()
    {
        var result = CliArgsParser.Parse(["verify", "./my-skill", "--version", "1.0.0"]);

        Assert.Equal("verify", result.Command);
        Assert.Equal("./my-skill", result.Positional[0]);
        Assert.Equal("1.0.0", result.VersionOverride);
    }
}
