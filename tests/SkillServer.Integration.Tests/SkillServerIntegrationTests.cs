// -----------------------------------------------------------------------
// <copyright file="SkillServerIntegrationTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

[Collection("SkillServer")]
public sealed class SkillServerIntegrationTests
{
    private readonly SkillServerFixture _fixture;

    public SkillServerIntegrationTests(SkillServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.HttpClient.GetAsync("/health", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRfcIndex_ReturnsIndex()
    {
        var ct = TestContext.Current.CancellationToken;
        var index = await _fixture.Client.GetRfcIndexAsync(ct);

        Assert.NotNull(index);
    }

    [Fact]
    public async Task ListSkills_ReturnsList()
    {
        var ct = TestContext.Current.CancellationToken;
        var skills = await _fixture.Client.ListSkillsAsync(ct: ct);

        Assert.NotNull(skills);
    }

    [Fact]
    public async Task UploadAndRetrieveSkill_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"e2e-test-{Guid.NewGuid():N}"[..20];

        // Upload a skill
        var skillContent = $"""
            ---
            name: {skillName}
            description: A test skill for integration testing
            ---

            # Test Skill

            This is a test skill.
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var uploadResponse = await _fixture.HttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        // List skills - should contain our skill
        var skills = await _fixture.Client.ListSkillsAsync(ct: ct);
        Assert.Contains(skills, s => s.Name == skillName);

        // Get RFC index - should contain our skill
        var index = await _fixture.Client.GetRfcIndexAsync(ct);
        Assert.NotNull(index);
        Assert.Contains(index.Skills, s => s.Name == skillName);

        // Get skill versions
        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Single(versions);
        Assert.Equal("1.0.0", versions[0].Version);

        // Get specific version
        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);
        Assert.Equal(skillName, version.Name);

        // Download SKILL.md
        var downloadedContent = await _fixture.Client.GetSkillFileAsStringAsync(skillName, "1.0.0", ct: ct);
        Assert.Contains("# Test Skill", downloadedContent);

        // Verify digest
        var verified = await _fixture.Client.VerifyDigestAsync(skillName, "1.0.0", version.Sha256, ct);
        Assert.True(verified);
    }

    [Fact]
    public async Task GetBlob_ByDigest()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"blob-test-{Guid.NewGuid():N}"[..20];

        // Upload a skill first
        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing blob endpoint
            ---

            # Blob Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        await _fixture.HttpClient.PostAsync("/skills", content, ct);

        // Get the version to find the digest
        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);

        // Download via blob endpoint
        await using var blobStream = await _fixture.Client.GetBlobAsync(version.Sha256, ct);
        using var reader = new StreamReader(blobStream);
        var blobContent = await reader.ReadToEndAsync(ct);

        Assert.Contains("# Blob Test", blobContent);
    }

    [Fact]
    public async Task DeleteVersion_RemovesSkill()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"del-test-{Guid.NewGuid():N}"[..20];

        // Upload a skill
        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing delete
            ---

            # Delete Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        await _fixture.HttpClient.PostAsync("/skills", content, ct);

        // Verify it exists
        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);

        // Delete it
        var deleteResponse = await _fixture.HttpClient.DeleteAsync($"/skills/{skillName}/1.0.0", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's gone
        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Empty(versions);
    }

    [Fact]
    public async Task Pagination_WorksCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"page-{Guid.NewGuid():N}"[..10];

        // Upload multiple skills
        for (int i = 1; i <= 3; i++)
        {
            var skillName = $"{prefix}-{i}";
            var skillContent = $"""
                ---
                name: {skillName}
                description: Pagination test skill {i}
                ---

                # Page Test {i}
                """;

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(skillName), "name");
            content.Add(new StringContent("1.0.0"), "version");

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
            content.Add(fileContent, "file", "SKILL.md");

            await _fixture.HttpClient.PostAsync("/skills", content, ct);
        }

        // Test pagination - get all skills then verify we can paginate
        var allSkills = await _fixture.Client.ListSkillsAsync(ct: ct);
        var ourSkills = allSkills.Where(s => s.Name.StartsWith(prefix)).ToList();
        Assert.Equal(3, ourSkills.Count);

        // Test take
        var limited = await _fixture.Client.ListSkillsAsync(take: 2, ct: ct);
        Assert.Equal(2, limited.Count);
    }
}
