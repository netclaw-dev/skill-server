// -----------------------------------------------------------------------
// <copyright file="ProgramDispatchTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Netclaw.SkillServer.Cli.Commands;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class ProgramDispatchTests : IDisposable
{
    private readonly string _tempDir;

    public ProgramDispatchTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skill-dispatch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Lint_DoesNotRequireServerUrl()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillDir = Path.Combine(_tempDir, "example-skill");
        Directory.CreateDirectory(skillDir);
        await File.WriteAllTextAsync(Path.Combine(skillDir, "SKILL.md"),
            "---\nname: example-skill\nversion: 1.0.0\ndescription: A skill.\n---\n\n# Example\n\nBody.",
            ct);

        var result = await RunCliAsync(["lint", _tempDir], ct);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Server URL not configured", result.StdErr);
        Assert.DoesNotContain("Server URL not configured", result.StdOut);
    }

    [Fact]
    public async Task LintSubAgent_DoesNotRequireServerUrl()
    {
        var ct = TestContext.Current.CancellationToken;
        var agentPath = Path.Combine(_tempDir, "support-agent.md");
        await File.WriteAllTextAsync(agentPath, """
            ---
            name: support-agent
            description: Diagnose support issues.
            ---

            You are a support diagnostician.
            """, ct);

        var result = await RunCliAsync(["lint", "subagent", agentPath], ct);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Server URL not configured", result.StdErr);
        Assert.DoesNotContain("Server URL not configured", result.StdOut);
    }

    [Fact]
    public async Task LintSubAgents_DoesNotRequireServerUrl()
    {
        var ct = TestContext.Current.CancellationToken;
        var agentPath = Path.Combine(_tempDir, "support-agent.md");
        await File.WriteAllTextAsync(agentPath, """
            ---
            name: support-agent
            description: Diagnose support issues.
            ---

            You are a support diagnostician.
            """, ct);

        var result = await RunCliAsync(["lint", "subagents", _tempDir], ct);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Server URL not configured", result.StdErr);
        Assert.DoesNotContain("Server URL not configured", result.StdOut);
    }

    [Fact]
    public async Task List_RequiresServerUrl()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await RunCliAsync(["list"], ct);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Server URL not configured", result.StdErr);
    }

    private static async Task<CliResult> RunCliAsync(string[] args, CancellationToken ct)
    {
        var dllPath = typeof(LintCommand).Assembly.Location;
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("exec");
        psi.ArgumentList.Add(dllPath);
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        // Isolate from the host environment: a developer or CI runner with these
        // variables set must not mask the bug this test guards against.
        psi.Environment.Remove("SKILLSERVER_URL");
        psi.Environment.Remove("SKILLSERVER_API_KEY");

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start CLI process");

        var stdOutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stdErrTask = proc.StandardError.ReadToEndAsync(ct);

        await proc.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromSeconds(30), ct);

        return new CliResult(proc.ExitCode, await stdOutTask, await stdErrTask);
    }

    private sealed record CliResult(int ExitCode, string StdOut, string StdErr);
}
