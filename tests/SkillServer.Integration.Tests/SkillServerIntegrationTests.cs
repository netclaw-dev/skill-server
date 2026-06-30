// -----------------------------------------------------------------------
// <copyright file="SkillServerIntegrationTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Security.Cryptography;
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

    private static string ComputeSha256Digest(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static MultipartFormDataContent CreateSubAgentUpload(string name, string version, string content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        form.Add(new StringContent(version), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        form.Add(fileContent, "file", "agent.md");

        return form;
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
        var indexSkill = Assert.Single(index.Skills, s => s.Name == skillName);
        Assert.Equal("skill-md", indexSkill.Type);
        Assert.EndsWith($"/skills/{skillName}/1.0.0/SKILL.md", indexSkill.Url, StringComparison.Ordinal);

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
    public async Task NativeManifest_SkillTraversal_ReturnsRfcArtifactValues()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"manifest-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Native manifest traversal test
            metadata:
              subagent: technical-support-diagnostician
            ---

            # Native Manifest Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var root = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeRootManifest>("/manifest.json", ct);
        Assert.NotNull(root);
        Assert.Equal("/manifest.json", root.Links.Self.Href);
        Assert.Equal("/.well-known/agent-skills/index.json", root.Links.RfcSkills.Href);
        Assert.Equal("/manifest/skills/index.json", root.Links.Skills.Href);

        var skillIndex = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeSkillCollectionIndex>(
            root.Links.Skills.Href, ct);
        Assert.NotNull(skillIndex);
        Assert.Equal("skill-index", skillIndex.Kind);
        var pageLink = Assert.Single(skillIndex.Pages);

        var skillPage = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeSkillCollectionPage>(
            pageLink.Href, ct);
        Assert.NotNull(skillPage);
        var item = Assert.Single(skillPage.Items, i => i.Name == skillName);
        Assert.Equal("1.0.0", item.LatestVersion);
        Assert.Equal($"/manifest/skills/{skillName}/index.json", item.Href);

        var identity = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeSkillIdentityIndex>(
            item.Href, ct);
        Assert.NotNull(identity);
        Assert.Equal(skillName, identity.Name);
        var versionLink = Assert.Single(identity.Versions, v => v.Version == "1.0.0");

        var detail = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeSkillVersionDetail>(
            versionLink.Href, ct);
        Assert.NotNull(detail);
        Assert.Equal("skill-version", detail.Kind);
        Assert.NotNull(detail.RoutesToSubagent);
        Assert.Equal("technical-support-diagnostician", detail.RoutesToSubagent!.Name);
        Assert.Equal("/manifest/subagents/technical-support-diagnostician/index.json", detail.RoutesToSubagent.Href);

        var rfcIndex = await _fixture.Client.GetRfcIndexAsync(ct);
        Assert.NotNull(rfcIndex);
        var rfcSkill = Assert.Single(rfcIndex.Skills, s => s.Name == skillName);
        Assert.Equal(rfcSkill.Name, detail.Artifact.Name);
        Assert.Equal(rfcSkill.Version, detail.Artifact.Version);
        Assert.Equal(rfcSkill.Type, detail.Artifact.Type);
        Assert.Equal(rfcSkill.Description, detail.Artifact.Description);
        Assert.Equal(rfcSkill.Url, detail.Artifact.Url);
        Assert.Equal(rfcSkill.Digest, detail.Artifact.Digest);
    }

    [Fact]
    public async Task UploadAndRetrieveSubAgent_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var subAgentName = $"agent-{Guid.NewGuid():N}"[..20];

        var agentMd = $"""
            ---
            name: {subAgentName}
            description: Diagnose technical support issues.
            modelRole: Main
            timeoutSeconds: 120
            prefillTimeoutSeconds: 30
            visibility: internal
            emitStructuredFindings: true
            ---

            You are a technical support diagnostician.
            """;

        using var content = CreateSubAgentUpload(subAgentName, "1.0.0", agentMd);
        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/subagents", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var upload = await uploadResponse.Content.ReadFromJsonAsync<SkillServer.Models.SubAgentUploadResponse>(ct);
        Assert.NotNull(upload);
        Assert.Equal(subAgentName, upload.Name);
        Assert.Equal("1.0.0", upload.Version);
        Assert.StartsWith("sha256:", upload.Sha256);
        Assert.EndsWith($"/subagents/{subAgentName}/1.0.0/agent.md", upload.Url, StringComparison.Ordinal);

        var list = await _fixture.HttpClient.GetFromJsonAsync<IReadOnlyList<SkillServer.Models.SubAgentSummary>>("/subagents", ct);
        Assert.NotNull(list);
        Assert.Contains(list, s => s.Name == subAgentName && s.LatestVersion == "1.0.0");

        var versions = await _fixture.HttpClient.GetFromJsonAsync<IReadOnlyList<SkillServer.Models.SubAgentVersionSummary>>(
            $"/subagents/{subAgentName}", ct);
        Assert.NotNull(versions);
        var version = Assert.Single(versions);
        Assert.Equal("agent-md", version.Type);
        Assert.Equal("Main", version.ModelRole);
        Assert.Equal(120, version.TimeoutSeconds);
        Assert.Equal(30, version.PrefillTimeoutSeconds);
        Assert.Equal("internal", version.Visibility);
        Assert.True(version.EmitStructuredFindings);

        var versionDetail = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.SubAgentVersionSummary>(
            $"/subagents/{subAgentName}/1.0.0", ct);
        Assert.NotNull(versionDetail);
        Assert.Equal(upload.Sha256, versionDetail.Sha256);

        var artifactResponse = await _fixture.HttpClient.GetAsync($"/subagents/{subAgentName}/1.0.0/agent.md", ct);
        Assert.Equal(HttpStatusCode.OK, artifactResponse.StatusCode);
        Assert.Equal("text/markdown", artifactResponse.Content.Headers.ContentType?.MediaType);

        var artifactBytes = await artifactResponse.Content.ReadAsByteArrayAsync(ct);
        Assert.Equal(upload.Sha256, ComputeSha256Digest(artifactBytes));
        Assert.Contains("technical support diagnostician", Encoding.UTF8.GetString(artifactBytes));

        var rfcIndex = await _fixture.Client.GetRfcIndexAsync(ct);
        Assert.NotNull(rfcIndex);
        Assert.DoesNotContain(rfcIndex.Skills, s => s.Name == subAgentName);
    }

    [Fact]
    public async Task UploadSubAgent_DuplicateVersion_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var subAgentName = $"dupe-{Guid.NewGuid():N}"[..20];
        var agentMd = $"---\nname: {subAgentName}\ndescription: Duplicate test\n---\nPrompt body";

        using var first = CreateSubAgentUpload(subAgentName, "1.0.0", agentMd);
        var firstResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/subagents", first, ct);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var second = CreateSubAgentUpload(subAgentName, "1.0.0", agentMd);
        var secondResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/subagents", second, ct);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UploadSubAgent_InvalidFrontmatter_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        var cases = new[]
        {
            (Name: $"missing-desc-{Guid.NewGuid():N}"[..20], Body: "description: ", Expected: "description"),
            (Name: $"empty-body-{Guid.NewGuid():N}"[..20], Body: "description: Empty body", Expected: "non-empty prompt body"),
            (Name: $"bad-role-{Guid.NewGuid():N}"[..20], Body: "description: Bad role\nmodelRole: Worker", Expected: "modelRole"),
            (Name: $"bad-timeout-{Guid.NewGuid():N}"[..20], Body: "description: Bad timeout\ntimeoutSeconds: 1", Expected: "timeoutSeconds"),
            (Name: $"bad-prefill-{Guid.NewGuid():N}"[..20], Body: "description: Bad prefill\nprefillTimeoutSeconds: 1", Expected: "prefillTimeoutSeconds"),
            (Name: $"bad-vis-{Guid.NewGuid():N}"[..20], Body: "description: Bad visibility\nvisibility: public", Expected: "visibility")
        };

        foreach (var testCase in cases)
        {
            var promptBody = testCase.Name.StartsWith("empty-body", StringComparison.Ordinal) ? "" : "Prompt body";
            var agentMd = $"---\nname: {testCase.Name}\n{testCase.Body}\n---\n{promptBody}";
            using var content = CreateSubAgentUpload(testCase.Name, "1.0.0", agentMd);

            var response = await _fixture.AuthenticatedHttpClient.PostAsync("/subagents", content, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadFromJsonAsync<SkillServer.Models.ErrorResponse>(ct);
            Assert.NotNull(error);
            Assert.Contains(testCase.Expected, error.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DeleteSubAgentVersion_RemovesVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var subAgentName = $"delete-agent-{Guid.NewGuid():N}"[..20];
        var agentMd = $"---\nname: {subAgentName}\ndescription: Delete test\n---\nPrompt body";

        using var content = CreateSubAgentUpload(subAgentName, "1.0.0", agentMd);
        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/subagents", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var deleteResponse = await _fixture.AuthenticatedHttpClient.DeleteAsync($"/subagents/{subAgentName}/1.0.0", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var versions = await _fixture.HttpClient.GetFromJsonAsync<IReadOnlyList<SkillServer.Models.SubAgentVersionSummary>>(
            $"/subagents/{subAgentName}", ct);
        Assert.NotNull(versions);
        Assert.Empty(versions);
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

    [Fact]
    public async Task UploadSkillWithResources_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"ref-test-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing resource file uploads
            ---

            # Resource Upload Test

            See references/guide.md for details.
            """;

        var referenceContent = """
            # Guide

            This is a reference document with detailed examples.

            ## Section One
            First section content.

            ## Section Two
            Second section content.
            """;

        var scriptContent = """
            #!/bin/bash
            echo "setup complete"
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var refFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(referenceContent));
        refFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(refFileContent, "resources", "references/guide.md");

        var scriptFileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(scriptContent));
        scriptFileContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-sh");
        content.Add(scriptFileContent, "resources", "scripts/setup.sh");

        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);
        Assert.Equal(2, version.FileCount);

        var downloadedSkill = await _fixture.Client.GetSkillFileAsStringAsync(skillName, "1.0.0", ct: ct);
        Assert.Contains("# Resource Upload Test", downloadedSkill);

        var refResponse = await _fixture.HttpClient.GetAsync(
            $"/skills/{skillName}/1.0.0/references/guide.md", ct);
        Assert.Equal(HttpStatusCode.OK, refResponse.StatusCode);
        var refBody = await refResponse.Content.ReadAsStringAsync(ct);
        Assert.Contains("# Guide", refBody);
        Assert.Contains("Section One", refBody);

        var scriptResponse = await _fixture.HttpClient.GetAsync(
            $"/skills/{skillName}/1.0.0/scripts/setup.sh", ct);
        Assert.Equal(HttpStatusCode.OK, scriptResponse.StatusCode);
        var scriptBody = await scriptResponse.Content.ReadAsStringAsync(ct);
        Assert.Contains("setup complete", scriptBody);

        var index = await _fixture.Client.GetRfcIndexAsync(ct);
        Assert.NotNull(index);
        var indexSkill = index.Skills.FirstOrDefault(s => s.Name == skillName);
        Assert.NotNull(indexSkill);
        Assert.Equal("archive", indexSkill!.Type);
        Assert.EndsWith($"/skills/{skillName}/1.0.0/archive.zip", indexSkill.Url, StringComparison.Ordinal);
        Assert.NotEqual(version.Sha256, indexSkill.Digest);

        var skillMdVerified = await _fixture.Client.VerifyDigestAsync(skillName, "1.0.0", version.Sha256, ct);
        Assert.True(skillMdVerified);

        var archiveResponse = await _fixture.HttpClient.GetAsync($"/skills/{skillName}/1.0.0/archive.zip", ct);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        Assert.Equal("application/zip", archiveResponse.Content.Headers.ContentType?.MediaType);

        var archiveBytes = await archiveResponse.Content.ReadAsByteArrayAsync(ct);
        Assert.Equal(indexSkill.Digest, ComputeSha256Digest(archiveBytes));

        var nativeDetail = await _fixture.HttpClient.GetFromJsonAsync<SkillServer.Models.NativeSkillVersionDetail>(
            $"/manifest/skills/{skillName}/versions/1.0.0.json", ct);
        Assert.NotNull(nativeDetail);
        Assert.Equal(indexSkill.Type, nativeDetail.Artifact.Type);
        Assert.Equal(indexSkill.Url, nativeDetail.Artifact.Url);
        Assert.Equal(indexSkill.Digest, nativeDetail.Artifact.Digest);

        using var archiveStream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        Assert.Equal([
            "SKILL.md",
            "references/guide.md",
            "scripts/setup.sh"
        ], archive.Entries.Select(e => e.FullName).ToArray());

        using var archivedSkillReader = new StreamReader(archive.GetEntry("SKILL.md")!.Open());
        var archivedSkill = await archivedSkillReader.ReadToEndAsync(ct);
        Assert.Contains("# Resource Upload Test", archivedSkill);

        var resources = indexSkill!.Resources;
        Assert.NotNull(resources);
        Assert.Equal(2, resources!.Count);
        Assert.Contains(resources, r => r.Path == "references/guide.md");
        Assert.Contains(resources, r => r.Path == "scripts/setup.sh");
    }

    [Fact]
    public async Task UploadSkillWithResources_WithoutResources_StillWorks()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"noref-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing upload without resources still works
            ---

            # No Resources Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var version = await _fixture.Client.GetVersionAsync(skillName, "1.0.0", ct);
        Assert.NotNull(version);
        Assert.Equal(0, version.FileCount);
    }

    [Fact]
    public async Task UploadSkillWithResources_InvalidPath_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"badpath-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Testing invalid resource path rejection
            ---

            # Bad Path Test
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(skillName), "name");
        content.Add(new StringContent("1.0.0"), "version");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(skillContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "SKILL.md");

        var badFile = new ByteArrayContent(Encoding.UTF8.GetBytes("exploit"));
        badFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(badFile, "resources", "../etc/passwd");

        var uploadResponse = await _fixture.AuthenticatedHttpClient.PostAsync("/skills", content, ct);
        Assert.Equal(HttpStatusCode.BadRequest, uploadResponse.StatusCode);
    }
}
