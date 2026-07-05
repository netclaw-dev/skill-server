// -----------------------------------------------------------------------
// <copyright file="SkillServerClientNativeManifestTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

[Collection("SkillServer")]
public sealed class SkillServerClientNativeManifestTests
{
    private readonly SkillServerFixture _fixture;

    public SkillServerClientNativeManifestTests(SkillServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ClientNativeManifestTraversal_DownloadsVerifiedSkillAndSubAgentArtifacts()
    {
        var ct = TestContext.Current.CancellationToken;
        var skillName = $"client-skill-{Guid.NewGuid():N}"[..20];
        var subAgentName = $"client-agent-{Guid.NewGuid():N}"[..20];

        var skillContent = $"""
            ---
            name: {skillName}
            description: Client native manifest traversal test
            metadata:
              subagent: {subAgentName}
            ---

            # Client Native Manifest Test
            """;

        var agentMd = $"""
            ---
            name: {subAgentName}
            description: Client native manifest sub-agent test.
            modelRole: Main
            timeoutSeconds: 120
            visibility: internal
            emitStructuredFindings: true
            ---

            You are a client-tested sub-agent.
            """;

        await using var skillStream = new MemoryStream(Encoding.UTF8.GetBytes(skillContent));
        await _fixture.AuthenticatedClient.UploadSkillAsync(skillName, "1.0.0", skillStream, ct: ct);

        await using var subAgentStream = new MemoryStream(Encoding.UTF8.GetBytes(agentMd));
        var subAgentUpload = await _fixture.AuthenticatedClient.UploadSubAgentAsync(subAgentName, "1.0.0", subAgentStream, ct);

        await using var duplicateSubAgentStream = new MemoryStream(Encoding.UTF8.GetBytes(agentMd));
        var duplicate = await _fixture.AuthenticatedClient.UploadSubAgentIfNotExistsAsync(
            subAgentName,
            "1.0.0",
            duplicateSubAgentStream,
            ct);
        Assert.Null(duplicate);

        var subAgents = await _fixture.Client.ListSubAgentsAsync(ct);
        Assert.Contains(subAgents, s => s.Name == subAgentName && s.LatestVersion == "1.0.0");

        var restVersions = await _fixture.Client.GetSubAgentVersionsAsync(subAgentName, ct);
        var restVersion = Assert.Single(restVersions);
        Assert.Equal(subAgentName, restVersion.Name);
        Assert.Equal("agent-md", restVersion.Type);
        Assert.Equal(subAgentUpload.Sha256, restVersion.Sha256);

        var restVersionDetail = await _fixture.Client.GetSubAgentVersionAsync(subAgentName, "1.0.0", ct);
        Assert.NotNull(restVersionDetail);
        Assert.Equal(subAgentUpload.Sha256, restVersionDetail.Sha256);

        var agentText = await _fixture.Client.GetSubAgentFileAsStringAsync(subAgentName, "1.0.0", ct);
        Assert.Contains("client-tested sub-agent", agentText);

        var root = await _fixture.Client.GetManifestAsync(ct);
        Assert.NotNull(root);
        Assert.Equal("/api/v1/manifest/skills/index.json", root.Links.Skills.Href);
        Assert.Equal("/api/v1/manifest/subagents/index.json", root.Links.SubAgents.Href);

        var skillIndex = await _fixture.Client.GetNativeSkillIndexAsync(root.Links.Skills, ct);
        Assert.NotNull(skillIndex);
        var skillPageLink = Assert.Single(skillIndex.Pages);
        var skillPage = await _fixture.Client.GetNativeSkillPageAsync(skillPageLink, ct);
        Assert.NotNull(skillPage);
        var skillItem = Assert.Single(skillPage.Items, i => i.Name == skillName);
        var skillIdentity = await _fixture.Client.GetNativeSkillIdentityAsync(skillItem, ct);
        Assert.NotNull(skillIdentity);
        var skillVersionLink = Assert.Single(skillIdentity.Versions, v => v.Version == "1.0.0");
        var skillDetail = await _fixture.Client.GetNativeSkillVersionAsync(skillVersionLink, ct);
        Assert.NotNull(skillDetail);
        Assert.Equal(subAgentName, skillDetail.RoutesToSubagent?.Name);

        await using var skillDestination = new MemoryStream();
        var skillDownload = await _fixture.Client.DownloadNativeSkillArtifactAsync(skillDetail.Artifact, skillDestination, ct);
        Assert.Equal(skillDetail.Artifact.Digest, skillDownload.Digest);
        Assert.Contains("# Client Native Manifest Test", Encoding.UTF8.GetString(skillDestination.ToArray()));

        var subAgentIndex = await _fixture.Client.GetNativeSubAgentIndexAsync(root.Links.SubAgents, ct);
        Assert.NotNull(subAgentIndex);
        var subAgentPageLink = Assert.Single(subAgentIndex.Pages);
        var subAgentPage = await _fixture.Client.GetNativeSubAgentPageAsync(subAgentPageLink, ct);
        Assert.NotNull(subAgentPage);
        var subAgentItem = Assert.Single(subAgentPage.Items, i => i.Name == subAgentName);
        var subAgentIdentity = await _fixture.Client.GetNativeSubAgentIdentityAsync(subAgentItem, ct);
        Assert.NotNull(subAgentIdentity);
        var subAgentVersionLink = Assert.Single(subAgentIdentity.Versions, v => v.Version == "1.0.0");
        var subAgentDetail = await _fixture.Client.GetNativeSubAgentVersionAsync(subAgentVersionLink, ct);
        Assert.NotNull(subAgentDetail);
        Assert.Equal(subAgentUpload.Sha256, subAgentDetail.Digest);

        await using var subAgentDestination = new MemoryStream();
        var subAgentDownload = await _fixture.Client.DownloadNativeSubAgentArtifactAsync(
            subAgentDetail,
            subAgentDestination,
            ct);
        Assert.Equal(subAgentDetail.Digest, subAgentDownload.Digest);
        Assert.Contains("client-tested sub-agent", Encoding.UTF8.GetString(subAgentDestination.ToArray()));

        await using var badDigestDestination = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => _fixture.Client.DownloadVerifiedArtifactAsync(
            subAgentDetail.Url,
            "sha256:0000000000000000000000000000000000000000000000000000000000000000",
            badDigestDestination,
            ct));
    }
}
