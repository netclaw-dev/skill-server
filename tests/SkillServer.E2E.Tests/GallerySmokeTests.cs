// -----------------------------------------------------------------------
// <copyright file="GalleryFixture.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace SkillServer.E2E.Tests;

public sealed class GalleryFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public HttpClient Client => _httpClient
        ?? throw new InvalidOperationException("HttpClient not initialized");

    public string ServiceEndpoint { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SkillServer>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        var endpoint = _app.GetEndpoint("skillserver", "http");
        ServiceEndpoint = endpoint.ToString().TrimEnd('/');

        // Wait for the server to accept connections
        var ready = await PollForEndpointReadyAsync(endpoint, TimeSpan.FromSeconds(30));
        if (!ready)
        {
            throw new TimeoutException($"Server at {endpoint} did not become ready within 30 seconds");
        }

        _httpClient = new HttpClient { BaseAddress = endpoint };
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
        _httpClient?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

[Collection("Gallery")]
public sealed class GallerySmokeTests : IClassFixture<GalleryFixture>
{
    private readonly GalleryFixture _fixture;

    public GallerySmokeTests(GalleryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GalleryHomePage_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("SkillServer", html);
    }

    [Fact]
    public async Task SkillsListing_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/skills/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("<title>Skills - SkillServer Gallery</title>", html);
        Assert.Contains("fetchSkillVersions", html);
    }

    [Fact]
    public async Task SubAgentsListing_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/subagents/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("<title>Sub-agents - SkillServer Gallery</title>", html);
        Assert.Contains("fetchSubAgentVersions", html);
    }

    [Fact]
    public async Task SkillDetailRoute_ServesSkillsShell()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/skills/dockerfile-hardening/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("<title>Skills - SkillServer Gallery</title>", html);
        Assert.Contains("fetchSkillVersions", html);
    }

    [Fact]
    public async Task SubAgentDetailRoute_ServesSubAgentsShell()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/subagents/static-analysis-auditor/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("<title>Sub-agents - SkillServer Gallery</title>", html);
        Assert.Contains("fetchSubAgentVersions", html);
    }

    [Fact]
    public async Task SkillsApi_ReturnsSeededSkills()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/api/v1/skills/", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("dockerfile-hardening", json);
        Assert.Contains("code-review-checklist", json);
    }

    [Fact]
    public async Task SubAgentsApi_ReturnsSeededSubAgents()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/api/v1/subagents/", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("static-analysis-auditor", json);
        Assert.Contains("dependency-impact-analyzer", json);
    }

    [Fact]
    public async Task StyleCss_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/style.css", ct);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ScriptJs_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/script.js", ct);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ApiJs_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/js/api.js", ct);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RouterJs_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/js/router.js", ct);
        response.EnsureSuccessStatusCode();
    }
}
