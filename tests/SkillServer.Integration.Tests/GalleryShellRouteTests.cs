// -----------------------------------------------------------------------
// <copyright file="GalleryShellRouteTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Xunit;

namespace SkillServer.Integration.Tests;

[Collection("SkillServer")]
public sealed class GalleryShellRouteTests
{
    private readonly SkillServerFixture _fixture;

    public GalleryShellRouteTests(SkillServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/skills/dockerfile-hardening/v/1.0.0/", "Skills")]
    [InlineData("/subagents/static-analysis-auditor/v/1.0.0/", "Sub-agents")]
    public async Task VersionDeepLink_ServesGalleryShell(string path, string title)
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.HttpClient.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains($"<title>{title} - SkillServer Gallery</title>", html);
    }

    [Theory]
    [InlineData("/favicon.ico", "image/x-icon")]
    [InlineData("/logo.svg", "image/svg+xml")]
    public async Task BrandIcon_IsServed(string path, string contentType)
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.HttpClient.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();

        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Content.Headers.ContentLength > 0);
    }
}
