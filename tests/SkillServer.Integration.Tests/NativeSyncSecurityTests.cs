// -----------------------------------------------------------------------
// <copyright file="NativeSyncSecurityTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

/// <summary>
/// Security and forward-compatibility hardening for the native manifest and
/// sub-agent sync surface (issue #93): authentication boundaries on write
/// endpoints, open read access to manifest/artifact endpoints, path-traversal
/// rejection on resource downloads, and digest verification on archive downloads.
/// </summary>
[Collection("SkillServer")]
public sealed class NativeSyncSecurityTests
{
    private readonly SkillServerFixture _fixture;

    public NativeSyncSecurityTests(SkillServerFixture fixture)
    {
        _fixture = fixture;
    }

    private static MultipartFormDataContent CreateSubAgentUpload(string name, string version)
    {
        var agentMd = $"""
            ---
            name: {name}
            description: Native sync security test sub-agent.
            ---

            You are a native-sync security test sub-agent.
            """;

        var form = new MultipartFormDataContent
        {
            { new StringContent(name), "name" },
            { new StringContent(version), "version" }
        };

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(agentMd));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        form.Add(fileContent, "file", "agent.md");

        return form;
    }

    private async Task<string> PublishResourcefulSkillAsync(string skillName, CancellationToken ct)
    {
        var skillMd = $"""
            ---
            name: {skillName}
            description: Native sync security test skill.
            ---

            # Security Test Skill

            See references/guide.md and scripts/setup.sh for details.
            """;

        using var content = new MultipartFormDataContent
        {
            { new StringContent(skillName), "name" },
            { new StringContent("1.0.0"), "version" }
        };

        var mdContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillMd));
        mdContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(mdContent, "file", "SKILL.md");

        content.Add(new ByteArrayContent("# Guide"u8.ToArray()), "resources", "references/guide.md");

        var response = await _fixture.AuthenticatedHttpClient.PostAsync("/api/v1/skills", content, ct);
        response.EnsureSuccessStatusCode();
        return skillName;
    }

    // ---- Task 4: write endpoints require authentication -----------------

    [Fact]
    public async Task PublishSubAgent_WithoutApiKey_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;

        using var content = CreateSubAgentUpload("no-auth-subagent", "1.0.0");
        var response = await _fixture.HttpClient.PostAsync("/api/v1/subagents", content, ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublishSubAgent_WithInvalidApiKey_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;

        using var content = CreateSubAgentUpload("bad-key-subagent", "1.0.0");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/subagents") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sk-invalid-key");

        var response = await _fixture.HttpClient.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSubAgent_WithoutApiKey_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _fixture.HttpClient.DeleteAsync("/api/v1/subagents/nonexistent/1.0.0", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Task 4: manifest + artifact reads stay open (auth is enabled) ---

    [Theory]
    [InlineData("/manifest.json")]
    [InlineData("/skills/v1/index.json")]
    [InlineData("/subagents/v1/index.json")]
    public async Task ManifestEndpoints_AreReadableWithoutAuth(string path)
    {
        var ct = TestContext.Current.CancellationToken;

        // The fixture's HttpClient carries no Authorization header, yet the
        // server is running with authentication enabled (see TestEnvironmentInitializer).
        var response = await _fixture.HttpClient.GetAsync(path, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ArtifactEndpoints_AreReadableWithoutAuth()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"sec-skill-{Guid.NewGuid():N}"[..20];
        var subAgentName = $"sec-agent-{Guid.NewGuid():N}"[..20];

        await PublishResourcefulSkillAsync(skillName, ct);

        using var subAgentUpload = CreateSubAgentUpload(subAgentName, "1.0.0");
        (await _fixture.AuthenticatedHttpClient.PostAsync("/api/v1/subagents", subAgentUpload, ct))
            .EnsureSuccessStatusCode();

        string[] openArtifacts =
        [
            $"/api/v1/skills/{skillName}/1.0.0/SKILL.md",
            $"/api/v1/skills/{skillName}/1.0.0/archive.zip",
            $"/api/v1/skills/{skillName}/1.0.0/references/guide.md",
            $"/api/v1/subagents/{subAgentName}/1.0.0/agent.md"
        ];

        foreach (var artifact in openArtifacts)
        {
            var response = await _fixture.HttpClient.GetAsync(artifact, ct);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 for unauthenticated GET {artifact}, got {(int)response.StatusCode}.");
        }
    }

    // ---- Task 3: resource downloads reject path traversal ---------------

    [Theory]
    [InlineData("..%2f..%2fSKILL.md")]
    [InlineData("references%2f..%2f..%2f..%2fappsettings.json")]
    public async Task SkillResourceDownload_WithTraversalPath_DoesNotEscape(string traversalPath)
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"trav-skill-{Guid.NewGuid():N}"[..20];

        await PublishResourcefulSkillAsync(skillName, ct);

        var response = await _fixture.HttpClient.GetAsync(
            $"/api/v1/skills/{skillName}/1.0.0/{traversalPath}", ct);

        // A traversal path must never resolve to a real file. Resource lookup is
        // by exact stored relative-path equality, so this yields a 404 rather than
        // leaking bytes outside the skill's resource set.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Task 2: archive downloads are digest-verified ------------------

    [Fact]
    public async Task DownloadVerifiedSkillArchive_WithTamperedDigest_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"digest-skill-{Guid.NewGuid():N}"[..20];

        await PublishResourcefulSkillAsync(skillName, ct);

        var detail = await _fixture.Client.GetNativeSkillVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(detail);
        // A resourceful skill is served as a deterministic archive artifact.
        Assert.Equal("archive", detail.Artifact.Type);

        await using var destination = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => _fixture.Client.DownloadVerifiedArtifactAsync(
            detail.Artifact.Url,
            "sha256:0000000000000000000000000000000000000000000000000000000000000000",
            destination,
            ct));
    }
}
