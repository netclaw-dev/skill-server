// -----------------------------------------------------------------------
// <copyright file="PublishSubAgentsCommandTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Commands;
using Netclaw.SkillServer.Cli.Publishing;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class PublishSubAgentsCommandTests : IDisposable
{
    private readonly string _tempDir;

    public PublishSubAgentsCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillserver-subagents-publish-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_DoesNotCallServer()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        CreateSubAgentFile("support-agent", version: "1.0.0");

        var exitCode = await PublishSubAgentsCommand.ExecuteAsync(
            new ParsedArgs { Positional = [_tempDir], DryRun = true },
            client);

        Assert.Equal(0, exitCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultVersion_PublishesFilesWithoutFrontmatterVersion()
    {
        var handler = new RecordingHandler(JsonResponse("support-agent", "1.2.3"));
        using var client = CreateClient(handler);
        CreateSubAgentFile("support-agent");

        var exitCode = await PublishSubAgentsCommand.ExecuteAsync(
            new ParsedArgs { Positional = [_tempDir], VersionOverride = "1.2.3" },
            client);

        Assert.Equal(0, exitCode);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/subagents", request.Path);
        Assert.Contains("support-agent", request.Body, StringComparison.Ordinal);
        Assert.Contains("1.2.3", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_FrontmatterVersion_OverridesDefaultVersion()
    {
        var handler = new RecordingHandler(JsonResponse("support-agent", "2.0.0"));
        using var client = CreateClient(handler);
        CreateSubAgentFile("support-agent", version: "2.0.0");

        var exitCode = await PublishSubAgentsCommand.ExecuteAsync(
            new ParsedArgs { Positional = [_tempDir], VersionOverride = "1.0.0" },
            client);

        Assert.Equal(0, exitCode);
        var request = Assert.Single(handler.Requests);
        Assert.Contains("2.0.0", request.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("1.0.0", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateNames_ReturnsErrorBeforePublishing()
    {
        var handler = new RecordingHandler();
        using var client = CreateClient(handler);
        CreateSubAgentFile("support-agent", fileName: "one.md", version: "1.0.0");
        CreateSubAgentFile("support-agent", fileName: "two.md", version: "1.0.1");

        var exitCode = await PublishSubAgentsCommand.ExecuteAsync(
            new ParsedArgs { Positional = [_tempDir] },
            client);

        Assert.Equal(1, exitCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void ValidateForPublish_MissingVersion_ReturnsIssue()
    {
        var path = CreateSubAgentFile("support-agent");
        var results = new[] { SubAgentFileScanner.ValidateFile(path) };

        var validation = PublishSubAgentsCommand.ValidateForPublish(results, defaultVersion: null);

        Assert.Empty(validation.Items);
        Assert.Contains(validation.Issues, issue => issue.Contains("Missing publication version", StringComparison.Ordinal));
    }

    private string CreateSubAgentFile(string name, string? fileName = null, string? version = null)
    {
        var path = Path.Combine(_tempDir, fileName ?? $"{name}.md");
        var versionLine = version is null ? "" : $"version: {version}\n";
        File.WriteAllText(path, $"""
            ---
            name: {name}
            {versionLine}description: Diagnose support issues.
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

    private static HttpResponseMessage JsonResponse(string name, string version)
    {
        var response = new SubAgentUploadResponse
        {
            Name = name,
            Version = version,
            Sha256 = "sha256:abc",
            Url = $"https://example.test/subagents/{name}/{version}/agent.md"
        };
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
