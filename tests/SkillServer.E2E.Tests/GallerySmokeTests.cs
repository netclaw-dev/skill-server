// -----------------------------------------------------------------------
// <copyright file="GalleryFixture.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SkillServer.E2E.Tests;

public sealed class GalleryFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    public HttpClient Client { get; private set; } = null!;
    public string BaseUrl { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();
        BaseUrl = Client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        return ValueTask.CompletedTask;
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
        Assert.Contains("gallery", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SkillsListing_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/skills/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("Skills", html);
    }

    [Fact]
    public async Task SubAgentsListing_LoadsSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/subagents/", ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("Sub-agents", html);
    }

    [Fact]
    public async Task SkillsApi_ReturnsJson()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/api/v1/skills/", ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SubAgentsApi_ReturnsJson()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.Client.GetAsync("/api/v1/subagents/", ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
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
