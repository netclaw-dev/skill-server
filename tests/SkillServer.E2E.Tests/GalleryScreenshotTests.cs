// -----------------------------------------------------------------------
// <copyright file="GalleryScreenshotTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Xunit;

namespace SkillServer.E2E.Tests;

[Collection("Gallery")]
public sealed class GalleryScreenshotTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IBrowser? _browser;
    private string _baseUrl = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SkillServer>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        var endpoint = _app.GetEndpoint("skillserver", "http");
        _baseUrl = endpoint.ToString().TrimEnd('/');

        var ready = await PollForEndpointReadyAsync(endpoint, TimeSpan.FromSeconds(30));
        if (!ready)
        {
            throw new TimeoutException($"Server at {endpoint} did not become ready within 30 seconds");
        }

        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync();
    }

    private static async Task<bool> PollForEndpointReadyAsync(Uri endpoint, TimeSpan timeout)
    {
        using var pollClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var success = await TryGetAsync(pollClient, "/");
            if (success)
            {
                return true;
            }
        }
        return false;
    }

    private static async Task<bool> TryGetAsync(HttpClient client, string requestUri)
    {
        try
        {
            using var response = await client.GetAsync(requestUri);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        if (_app is not null) await _app.DisposeAsync();
    }

    private async Task<(string path, List<string> errors, List<string> failedRequests, string bodyBackground)> ScreenshotPageAsync(
        string screenshotName, string url, ViewportSize? viewport = null)
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = viewport ?? new ViewportSize { Width = 1280, Height = 900 }
        });
        var page = await context.NewPageAsync();

        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
                consoleErrors.Add(msg.Text);
        };
        page.RequestFailed += (_, req) =>
        {
            failedRequests.Add($"{req.Method} {req.Url} — {req.Failure}");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                failedRequests.Add($"{response.Status} {response.Url}");
            }
        };

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var bodyBackground = await page.Locator("body")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        var screenshotPath = Path.Combine(Path.GetTempPath(), $"{screenshotName}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
        await context.CloseAsync();
        return (screenshotPath, consoleErrors, failedRequests, bodyBackground);
    }

    [Fact]
    public async Task Screenshot_HomePage()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-home", $"{_baseUrl}/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SkillsListing()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-skills-listing", $"{_baseUrl}/skills/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SkillDetail_DockerfileHardening()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-skill-dockerfile-hardening",
            $"{_baseUrl}/skills/dockerfile-hardening/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SkillDetail_CodeReviewChecklist()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-skill-code-review-checklist",
            $"{_baseUrl}/skills/code-review-checklist/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SubAgentsListing()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-subagents-listing", $"{_baseUrl}/subagents/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SubAgentDetail_StaticAnalysisAuditor()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-subagent-static-analysis-auditor",
            $"{_baseUrl}/subagents/static-analysis-auditor/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task Screenshot_SubAgentDetail_DependencyImpactAnalyzer()
    {
        var (path, errors, failed, bodyBackground) = await ScreenshotPageAsync("gallery-subagent-dependency-impact-analyzer",
            $"{_baseUrl}/subagents/dependency-impact-analyzer/");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        Assert.Equal("rgb(26, 26, 46)", bodyBackground);
        foreach (var e in errors) Console.WriteLine($"CONSOLE ERROR: {e}");
        foreach (var f in failed) Console.WriteLine($"FAILED REQUEST: {f}");
        Assert.Empty(errors);
        Assert.Empty(failed);
    }
}
