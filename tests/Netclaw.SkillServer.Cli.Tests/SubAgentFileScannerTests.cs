// -----------------------------------------------------------------------
// <copyright file="SubAgentFileScannerTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Publishing;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class SubAgentFileScannerTests
{
    [Fact]
    public void ValidateContent_ValidSubAgent_ReturnsScannedSubAgent()
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", """
            ---
            name: support-agent
            description: Diagnose support issues.
            modelRole: Main
            timeoutSeconds: 120
            prefillTimeoutSeconds: 30
            visibility: internal
            emitStructuredFindings: true
            ---

            You are a support diagnostician.
            """);

        Assert.Empty(result.Issues);
        Assert.Empty(result.Warnings);
        Assert.NotNull(result.SubAgent);
        Assert.Equal("support-agent", result.SubAgent.Name);
        Assert.Equal("Main", result.SubAgent.ModelRole);
        Assert.Equal(120, result.SubAgent.TimeoutSeconds);
        Assert.Equal(30, result.SubAgent.PrefillTimeoutSeconds);
        Assert.Equal("internal", result.SubAgent.Visibility);
        Assert.True(result.SubAgent.EmitStructuredFindings);
    }

    [Fact]
    public void ValidateContent_DefaultOptionalFields_MatchesServerDefaults()
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", """
            ---
            name: support-agent
            description: Diagnose support issues.
            ---

            Prompt body.
            """);

        Assert.Empty(result.Issues);
        Assert.NotNull(result.SubAgent);
        Assert.Equal("Compaction", result.SubAgent.ModelRole);
        Assert.Equal(60, result.SubAgent.TimeoutSeconds);
        Assert.Null(result.SubAgent.PrefillTimeoutSeconds);
        Assert.Equal("user-facing", result.SubAgent.Visibility);
        Assert.False(result.SubAgent.EmitStructuredFindings);
    }

    [Theory]
    [InlineData("", "Missing required 'name'")]
    [InlineData("-bad", "Invalid 'name' format")]
    [InlineData("bad--name", "Invalid 'name' format")]
    public void ValidateContent_InvalidNames_ReportIssue(string name, string expected)
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", $"""
            ---
            name: {name}
            description: Diagnose support issues.
            ---

            Prompt body.
            """);

        Assert.Contains(result.Issues, issue => issue.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateContent_InvalidOptionalValues_ReportIssues()
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", """
            ---
            name: support-agent
            description: Diagnose support issues.
            modelRole: Worker
            timeoutSeconds: 2
            prefillTimeoutSeconds: 9999
            visibility: public
            emitStructuredFindings: sometimes
            ---

            Prompt body.
            """);

        Assert.Contains(result.Issues, issue => issue.Contains("Invalid modelRole", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue => issue.Contains("Invalid timeoutSeconds", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue => issue.Contains("Invalid prefillTimeoutSeconds", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue => issue.Contains("Invalid visibility", StringComparison.Ordinal));
        Assert.Contains(result.Issues, issue => issue.Contains("Invalid emitStructuredFindings", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateContent_UnknownField_WarnsOnly()
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", """
            ---
            name: support-agent
            description: Diagnose support issues.
            customField: value
            ---

            Prompt body.
            """);

        Assert.Empty(result.Issues);
        Assert.Single(result.Warnings);
        Assert.Contains("customField", result.Warnings[0]);
    }

    [Fact]
    public void ValidateContent_EmptyBody_ReportIssue()
    {
        var result = SubAgentFileScanner.ValidateContent("agent.md", "/tmp/agent.md", """
            ---
            name: support-agent
            description: Diagnose support issues.
            ---
            """);

        Assert.Contains(result.Issues, issue => issue.Contains("prompt body", StringComparison.Ordinal));
    }
}
