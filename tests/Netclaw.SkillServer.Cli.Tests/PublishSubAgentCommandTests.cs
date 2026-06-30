// -----------------------------------------------------------------------
// <copyright file="PublishSubAgentCommandTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Commands;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class PublishSubAgentCommandTests : IDisposable
{
    private readonly string _tempDir;

    public PublishSubAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillserver-subagent-publish-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_ValidFile_PostsSubAgent()
    {
        var handler = new RecordingHandler(JsonResponse(new SubAgentUploadResponse
        {
            Name = "support-agent",
            Version = "1.0.0",
            Sha256 = "sha256:abc",
            Url = "https://example.test/subagents/support-agent/1.0.0/agent.md"
        }));
        using var client = CreateClient(handler);
        var path = CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = [path], VersionOverride = "1.0.0", Verbose = true },
            client);

        Assert.Equal(0, exitCode);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/subagents", request.Path);
        Assert.Contains("support-agent", request.Body);
        Assert.Contains("1.0.0", request.Body);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateVersion_ReturnsSuccessWithoutThrowing()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Conflict));
        using var client = CreateClient(handler);
        var path = CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = [path], VersionOverride = "1.0.0" },
            client);

        Assert.Equal(0, exitCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_Force_DeletesBeforeUpload()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.NoContent),
            JsonResponse(new SubAgentUploadResponse
            {
                Name = "support-agent",
                Version = "1.0.0",
                Sha256 = "sha256:abc",
                Url = "https://example.test/subagents/support-agent/1.0.0/agent.md"
            }));
        using var client = CreateClient(handler);
        var path = CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = [path], VersionOverride = "1.0.0", Force = true },
            client);

        Assert.Equal(0, exitCode);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("/subagents/support-agent/1.0.0", handler.Requests[0].Path);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/subagents", handler.Requests[1].Path);
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_DoesNotCallServer()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        var path = CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = [path], VersionOverride = "1.0.0", DryRun = true },
            client);

        Assert.Equal(0, exitCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_MissingVersion_ReturnsError()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        var path = CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentCommand.ExecuteAsync(
            new ParsedArgs { Positional = [path] },
            client);

        Assert.Equal(1, exitCode);
        Assert.Empty(handler.Requests);
    }

    private string CreateSubAgentFile(string name)
    {
        var path = Path.Combine(_tempDir, $"{name}.md");
        File.WriteAllText(path, $"""
            ---
            name: {name}
            description: Diagnose support issues.
            modelRole: Main
            timeoutSeconds: 120
            visibility: internal
            ---

            You are a support diagnostician.
            """);
        return path;
    }

    private static SkillServerClient CreateClient(HttpMessageHandler handler)
    {
        return new SkillServerClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        });
    }

    private static HttpResponseMessage JsonResponse(SubAgentUploadResponse response)
    {
        var json = JsonSerializer.Serialize(response, SkillServerClientJsonContext.Default.SubAgentUploadResponse);
        return new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.AbsolutePath ?? "",
                body));

            return _responses.Count == 0
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);
}
