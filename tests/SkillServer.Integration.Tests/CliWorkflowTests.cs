// -----------------------------------------------------------------------
// <copyright file="CliWorkflowTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

[Collection("SkillServer")]
public sealed class CliWorkflowTests
{
    private readonly SkillServerFixture _fixture;

    public CliWorkflowTests(SkillServerFixture fixture)
    {
        _fixture = fixture;
    }

    private static MemoryStream ToStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task PublishListVerifyDelete_FullWorkflow()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-full-{Guid.NewGuid():N}"[..20];

        var skillMd = $"""
            ---
            name: {skillName}
            version: 1.0.0
            category: testing
            description: Full workflow test
            ---

            # {skillName}

            Workflow test content.
            """;

        // Publish (auth required)
        using var stream = ToStream(skillMd);
        var uploadResponse = await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", stream, [], "testing", ct);

        Assert.Equal(skillName, uploadResponse.Name);
        Assert.Equal("1.0.0", uploadResponse.Version);
        Assert.StartsWith("sha256:", uploadResponse.Sha256);

        // List — no auth needed
        var skills = await _fixture.Client.ListSkillsAsync(ct: ct);
        Assert.Contains(skills, s => s.Name == skillName);

        // Versions — no auth needed
        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Single(versions);
        Assert.Equal("1.0.0", versions[0].Version);
        Assert.True(versions[0].IsLatest);

        // Verify digest — no auth needed
        var verified = await _fixture.Client.VerifyDigestAsync(
            skillName, "1.0.0", uploadResponse.Sha256, ct);
        Assert.True(verified);

        // Download — no auth needed
        var downloaded = await _fixture.Client.GetSkillFileAsStringAsync(
            skillName, "1.0.0", ct: ct);
        Assert.Contains("Workflow test content.", downloaded);

        // Delete (auth required)
        await _fixture.AuthenticatedClient.DeleteVersionAsync(skillName, "1.0.0", ct);

        // Verify gone
        var versionsAfter = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Empty(versionsAfter);
    }

    [Fact]
    public async Task IdempotentUpload_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-idem-{Guid.NewGuid():N}"[..20];

        var skillMd = $"---\nname: {skillName}\nversion: 1.0.0\ndescription: test\n---\n# Test";

        using var stream1 = ToStream(skillMd);
        var first = await _fixture.AuthenticatedClient.UploadSkillIfNotExistsAsync(
            skillName, "1.0.0", stream1, [], ct: ct);
        Assert.NotNull(first);

        using var stream2 = ToStream(skillMd);
        var second = await _fixture.AuthenticatedClient.UploadSkillIfNotExistsAsync(
            skillName, "1.0.0", stream2, [], ct: ct);
        Assert.Null(second);
    }

    [Fact]
    public async Task ForceRepublish_DeleteThenUpload()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-force-{Guid.NewGuid():N}"[..20];

        using var stream1 = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: v1\n---\n# V1");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", stream1, [], ct: ct);

        await _fixture.AuthenticatedClient.DeleteVersionAsync(skillName, "1.0.0", ct);

        using var stream2 = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: v1-updated\n---\n# V1 Updated");
        var second = await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", stream2, [], ct: ct);
        Assert.NotNull(second);

        var downloaded = await _fixture.Client.GetSkillFileAsStringAsync(
            skillName, "1.0.0", ct: ct);
        Assert.Contains("V1 Updated", downloaded);

        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Single(versions);
    }

    [Fact]
    public async Task PublishWithResources_AllAccessible()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-res-{Guid.NewGuid():N}"[..20];

        var resources = new List<(string RelativePath, Stream Content)>
        {
            ("references/patterns.md", ToStream("# Patterns\nContent here.")),
            ("references/examples.md", ToStream("# Examples\nMore content.")),
            ("scripts/setup.sh", ToStream("#!/bin/bash\necho hello"))
        };

        using var skillStream = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: test\n---\n# {skillName}");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", skillStream, resources, ct: ct);

        var patterns = await _fixture.Client.GetSkillFileAsStringAsync(
            skillName, "1.0.0", "references/patterns.md", ct);
        Assert.Contains("# Patterns", patterns);

        var examples = await _fixture.Client.GetSkillFileAsStringAsync(
            skillName, "1.0.0", "references/examples.md", ct);
        Assert.Contains("# Examples", examples);

        var setup = await _fixture.Client.GetSkillFileAsStringAsync(
            skillName, "1.0.0", "scripts/setup.sh", ct);
        Assert.Contains("echo hello", setup);

        foreach (var (_, stream) in resources)
            stream.Dispose();
    }

    [Fact]
    public async Task MultipleVersions_LatestTracked()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-ver-{Guid.NewGuid():N}"[..20];

        using var v1 = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: test\n---\n# V1");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", v1, [], ct: ct);

        using var v2 = ToStream(
            $"---\nname: {skillName}\nversion: 2.0.0\ndescription: test\n---\n# V2");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "2.0.0", v2, [], ct: ct);

        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Equal(2, versions.Count);

        var latest = await _fixture.Client.GetLatestVersionAsync(skillName, ct);
        Assert.NotNull(latest);
        Assert.Equal("2.0.0", latest.Version);

        var updates = await _fixture.Client.CheckUpdatesAsync([
            new CheckUpdateRequest { Name = skillName, Version = "1.0.0" }
        ], ct);
        Assert.Single(updates);
        Assert.True(updates[0].HasUpdate);
        Assert.Equal("2.0.0", updates[0].LatestVersion);
    }

    [Fact]
    public async Task SearchSkills_FindsByName()
    {
        var ct = TestContext.Current.CancellationToken;
        var uniqueToken = Guid.NewGuid().ToString("N")[..8];
        var skillName = $"wf-srch-{uniqueToken}";

        using var stream = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: Searchable skill\n---\n# {skillName}");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", stream, [], ct: ct);

        var results = await _fixture.Client.SearchSkillsAsync(uniqueToken, ct: ct);
        Assert.Contains(results, s => s.Name == skillName);
    }

    [Fact]
    public async Task DigestMismatch_Detected()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"wf-dig-{Guid.NewGuid():N}"[..20];

        using var stream = ToStream(
            $"---\nname: {skillName}\nversion: 1.0.0\ndescription: test\n---\n# Test");
        await _fixture.AuthenticatedClient.UploadSkillWithResourcesAsync(
            skillName, "1.0.0", stream, [], ct: ct);

        var mismatch = await _fixture.Client.VerifyDigestAsync(
            skillName, "1.0.0", "sha256:0000000000000000000000000000000000000000000000000000000000000000", ct);
        Assert.False(mismatch);
    }

    [Fact]
    public async Task DeleteNonexistent_Throws()
    {
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _fixture.AuthenticatedClient.DeleteVersionAsync("nonexistent-skill", "99.99.99", ct));
    }

    [Fact]
    public async Task ApiKeyManagement_ViaClient()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await _fixture.AuthenticatedClient.CreateApiKeyAsync("e2e-test-key", ct: ct);
        Assert.NotEmpty(created.Key);
        Assert.Equal("e2e-test-key", created.Label);

        var keys = await _fixture.AuthenticatedClient.ListApiKeysAsync(ct);
        Assert.Contains(keys, k => k.Id == created.Id);

        await _fixture.AuthenticatedClient.DeleteApiKeyAsync(created.Id, ct);

        var keysAfter = await _fixture.AuthenticatedClient.ListApiKeysAsync(ct);
        Assert.DoesNotContain(keysAfter, k => k.Id == created.Id);
    }
}
