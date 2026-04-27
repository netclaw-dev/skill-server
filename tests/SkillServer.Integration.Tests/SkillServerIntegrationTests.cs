// -----------------------------------------------------------------------
// <copyright file="SkillServerIntegrationTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

        var body = await response.Content.ReadFromJsonAsync<SkillServer.Models.HealthResponse>(ct);
        Assert.NotNull(body);
        Assert.Equal("healthy", body.Status);
        Assert.True(body.Timestamp > DateTimeOffset.MinValue);
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

        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        // List skills - should contain our skill (no auth required for reads)
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

        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);

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

        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);

        // Verify it exists
        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);

        // Delete it (requires auth)
        var deleteResponse = await _fixture.AuthenticatedHttpClient.DeleteAsync($"/skills/{skillName}/1.0.0", ct);
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

            await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        }

        // Test pagination - get all skills then verify we can paginate
        var allSkills = await _fixture.Client.ListSkillsAsync(ct: ct);
        var ourSkills = allSkills.Where(s => s.Name.StartsWith(prefix)).ToList();
        Assert.Equal(3, ourSkills.Count);

        // Test take
        var limited = await _fixture.Client.ListSkillsAsync(take: 2, ct: ct);
        Assert.Equal(2, limited.Count);
    }

    [Fact]
    public async Task UploadSkill_WithoutApiKey_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("no-auth-test"), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent("---\nname: no-auth-test\ndescription: test\n---\n# Test"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var response = await _fixture.HttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSkill_WithoutApiKey_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _fixture.HttpClient.DeleteAsync("/skills/nonexistent/1.0.0", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadSkill_WithInvalidApiKey_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("bad-key-test"), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent("---\nname: bad-key-test\ndescription: test\n---\n# Test"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/skills");
        request.Content = content;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sk-invalid-key");

        var response = await _fixture.HttpClient.SendAsync(request, ct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSkills_WithoutApiKey_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _fixture.HttpClient.GetAsync("/skills", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyManagement_CreateListDelete_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a new key
        var createResponse = await _fixture.AuthenticatedHttpClient.PostAsJsonAsync("/api-keys",
            new { label = "test-key" }, ct);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = SkillServerClientJsonContext.Default
        };
        var created = await createResponse.Content.ReadFromJsonAsync<Netclaw.SkillClient.CreateApiKeyResponse>(jsonOptions, ct);
        Assert.NotNull(created);
        Assert.Equal("test-key", created.Label);
        Assert.StartsWith("sk-", created.Key);

        // List keys
        var listResponse = await _fixture.AuthenticatedHttpClient.GetAsync("/api-keys", ct);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var keys = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<Netclaw.SkillClient.ApiKeySummary>>(jsonOptions, ct);
        Assert.NotNull(keys);
        Assert.Contains(keys, k => k.Label == "test-key");

        // Delete the new key (not the bootstrap key)
        var deleteResponse = await _fixture.AuthenticatedHttpClient.DeleteAsync($"/api-keys/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task SearchSkills_ReturnsMatchingResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"srch-{Guid.NewGuid():N}"[..10];
        var skillName = $"{prefix}-finder";

        // Upload a skill with a distinctive description
        var skillContent = $"""
            ---
            name: {skillName}
            description: Helps with kubernetes pod deployment orchestration
            ---

            # Search Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);

        // Search by description keyword
        var results = await _fixture.Client.SearchSkillsAsync("kubernetes", ct: ct);
        Assert.Contains(results, s => s.Name == skillName);

        // Search by name
        results = await _fixture.Client.SearchSkillsAsync(prefix, ct: ct);
        Assert.Contains(results, s => s.Name == skillName);

        // Search for non-existent term
        results = await _fixture.Client.SearchSkillsAsync("zzz-nonexistent-zzz", ct: ct);
        Assert.DoesNotContain(results, s => s.Name == skillName);
    }

    [Fact]
    public async Task GetLatestVersion_ReturnsLatest()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"latest-{Guid.NewGuid():N}"[..20];

        // Upload v1.0.0
        var skillContent1 = $"""
            ---
            name: {skillName}
            description: Version one
            ---

            # V1
            """;

        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent(skillName), "name");
        content1.Add(new StringContent("1.0.0"), "version");
        var fileContent1 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent1));
        fileContent1.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content1.Add(fileContent1, "file", "SKILL.md");
        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content1, ct);

        // Upload v2.0.0
        var skillContent2 = $"""
            ---
            name: {skillName}
            description: Version two
            ---

            # V2
            """;

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent(skillName), "name");
        content2.Add(new StringContent("2.0.0"), "version");
        var fileContent2 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent2));
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content2.Add(fileContent2, "file", "SKILL.md");
        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content2, ct);

        // Get latest - should be v2.0.0
        var latest = await _fixture.Client.GetLatestVersionAsync(skillName, ct);
        Assert.NotNull(latest);
        Assert.Equal("2.0.0", latest.Version);
        Assert.True(latest.IsLatest);
    }

    [Fact]
    public async Task GetLatestVersion_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _fixture.HttpClient.GetAsync("/skills/nonexistent-skill/latest", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckUpdates_ReturnsUpdateStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"upd-{Guid.NewGuid():N}"[..20];

        // Upload v1.0.0
        var skillContent1 = $"""
            ---
            name: {skillName}
            description: Update check test
            ---

            # V1
            """;

        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent(skillName), "name");
        content1.Add(new StringContent("1.0.0"), "version");
        var fileContent1 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent1));
        fileContent1.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content1.Add(fileContent1, "file", "SKILL.md");
        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content1, ct);

        // Upload v2.0.0
        var skillContent2 = $"""
            ---
            name: {skillName}
            description: Update check test v2
            ---

            # V2
            """;

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent(skillName), "name");
        content2.Add(new StringContent("2.0.0"), "version");
        var fileContent2 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent2));
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content2.Add(fileContent2, "file", "SKILL.md");
        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content2, ct);

        // Check updates - asking about v1.0.0 should show update available
        var updates = await _fixture.Client.CheckUpdatesAsync([
            new Netclaw.SkillClient.CheckUpdateRequest { Name = skillName, Version = "1.0.0" },
            new Netclaw.SkillClient.CheckUpdateRequest { Name = "nonexistent-skill", Version = "1.0.0" }
        ], ct);

        Assert.Equal(2, updates.Count);

        var skillUpdate = updates.First(u => u.Name == skillName);
        Assert.True(skillUpdate.HasUpdate);
        Assert.Equal("1.0.0", skillUpdate.CurrentVersion);
        Assert.Equal("2.0.0", skillUpdate.LatestVersion);

        var missingSkill = updates.First(u => u.Name == "nonexistent-skill");
        Assert.False(missingSkill.HasUpdate);
    }

    [Fact]
    public async Task ApiKeyManagement_WithoutAuth_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _fixture.HttpClient.GetAsync("/api-keys", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadSkill_DuplicateVersion_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"dup-test-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing duplicate version
            ---

            # Duplicate Test
            """;

        // Upload the first version
        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent(skillName), "name");
        content1.Add(new StringContent("1.0.0"), "version");

        var fileContent1 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent1.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content1.Add(fileContent1, "file", "SKILL.md");

        var uploadResponse1 = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content1, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse1.StatusCode);

        // Upload the same version again - should return 409 Conflict
        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent(skillName), "name");
        content2.Add(new StringContent("1.0.0"), "version");

        var fileContent2 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content2.Add(fileContent2, "file", "SKILL.md");

        var uploadResponse2 = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content2, ct);
        Assert.Equal(HttpStatusCode.Conflict, uploadResponse2.StatusCode);

        // Verify the response body contains the duplicate_version error
        var errorBody = await uploadResponse2.Content.ReadFromJsonAsync<SkillServer.Models.ErrorResponse>(ct);
        Assert.NotNull(errorBody);
        Assert.Equal("duplicate_version", errorBody.Error);
        Assert.Contains("already exists", errorBody.Message!);
    }

    [Fact]
    public async Task UploadSkill_DifferentVersions_AreAllowed()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"ver-test-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing different versions
            ---

            # Version Test
            """;

        // Upload v1.0.0
        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent(skillName), "name");
        content1.Add(new StringContent("1.0.0"), "version");

        var fileContent1 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent1.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content1.Add(fileContent1, "file", "SKILL.md");

        var uploadResponse1 = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content1, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse1.StatusCode);

        // Upload v2.0.0 - should succeed
        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent(skillName), "name");
        content2.Add(new StringContent("2.0.0"), "version");

        var fileContent2 = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content2.Add(fileContent2, "file", "SKILL.md");

        var uploadResponse2 = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content2, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse2.StatusCode);

        // Verify both versions exist
        var versions = await _fixture.Client.GetSkillVersionsAsync(skillName, ct);
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public async Task SearchSkills_WithPorterStemming_MatchesStemmedTerms()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"stem-{Guid.NewGuid():N}"[..10];
        var skillName = $"{prefix}-closer";

        var skillContent = $"""
            ---
            name: {skillName}
            description: Helps sales reps close deal opportunities faster
            ---

            # Stemming Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);

        // "closed deals" should match "close deal" via porter stemming
        var results = await _fixture.Client.SearchSkillsAsync("closed deals", ct: ct);
        Assert.Contains(results, s => s.Name == skillName);

        // "closing" should match "close" via stemming
        results = await _fixture.Client.SearchSkillsAsync("closing", ct: ct);
        Assert.Contains(results, s => s.Name == skillName);
    }
}
