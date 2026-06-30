// -----------------------------------------------------------------------
// <copyright file="DownloadSubAgentCommandTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Commands;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class DownloadSubAgentCommandTests : IDisposable
{
    private readonly string _tempDir;

    public DownloadSubAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillserver-subagent-download-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadsVerifiedArtifactToDestination()
    {
        var agentMd = "---\nname: support-agent\ndescription: test\n---\nPrompt body.";
        var digest = ComputeSha256Digest(agentMd);
        var handler = new RecordingHandler(
            JsonResponse(CreateDetail(digest)),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(agentMd, Encoding.UTF8, "text/markdown")
            });
        using var client = CreateClient(handler);
        var destination = Path.Combine(_tempDir, "support-agent.md");

        var exitCode = await DownloadSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["support-agent", "1.0.0", destination], Verbose = true },
            client);

        Assert.Equal(0, exitCode);
        Assert.Equal(agentMd, await File.ReadAllTextAsync(destination, TestContext.Current.CancellationToken));
        Assert.Equal("/manifest/subagents/support-agent/versions/1.0.0.json", handler.Requests[0].Path);
        Assert.Equal("/subagents/support-agent/1.0.0/agent.md", handler.Requests[1].Path);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingDestinationWithoutForce_DoesNotCallServer()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        var destination = Path.Combine(_tempDir, "support-agent.md");
        await File.WriteAllTextAsync(destination, "existing", TestContext.Current.CancellationToken);

        var exitCode = await DownloadSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["support-agent", "1.0.0", destination] },
            client);

        Assert.Equal(1, exitCode);
        Assert.Empty(handler.Requests);
        Assert.Equal("existing", await File.ReadAllTextAsync(destination, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_DoesNotCallServerOrWriteFile()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        var destination = Path.Combine(_tempDir, "support-agent.md");

        var exitCode = await DownloadSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["support-agent", "1.0.0", destination], DryRun = true },
            client);

        Assert.Equal(0, exitCode);
        Assert.Empty(handler.Requests);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task ExecuteAsync_DigestMismatch_DoesNotLeaveDestinationFile()
    {
        var handler = new RecordingHandler(
            JsonResponse(CreateDetail("sha256:0000000000000000000000000000000000000000000000000000000000000000")),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("wrong bytes", Encoding.UTF8, "text/markdown")
            });
        using var client = CreateClient(handler);
        var destination = Path.Combine(_tempDir, "support-agent.md");

        var exitCode = await DownloadSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["support-agent", "1.0.0", destination] },
            client);

        Assert.Equal(1, exitCode);
        Assert.False(File.Exists(destination));
        Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp-*"));
    }

    private static NativeSubAgentVersionDetail CreateDetail(string digest) => new()
    {
        Kind = "subagent-version",
        Name = "support-agent",
        Version = "1.0.0",
        Type = SubAgentArtifactTypes.AgentMd,
        Description = "test",
        Url = "/subagents/support-agent/1.0.0/agent.md",
        Digest = digest,
        Links = new NativeManifestSelfLinks
        {
            Self = new NativeManifestLink
            {
                Href = "/manifest/subagents/support-agent/versions/1.0.0.json"
            }
        }
    };

    private static SkillServerClient CreateClient(HttpMessageHandler handler)
    {
        return new SkillServerClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        });
    }

    private static HttpResponseMessage JsonResponse(NativeSubAgentVersionDetail detail)
    {
        var json = JsonSerializer.Serialize(detail, SkillServerClientJsonContext.Default.NativeSubAgentVersionDetail);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string ComputeSha256Digest(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.AbsolutePath ?? ""));

            return Task.FromResult(_responses.Count == 0
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : _responses.Dequeue());
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path);
}
