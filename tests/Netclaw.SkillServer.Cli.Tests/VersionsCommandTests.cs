// -----------------------------------------------------------------------
// <copyright file="VersionsCommandTests.cs" company="Petabridge, LLC">
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

public sealed class VersionsCommandTests
{
    [Fact]
    public async Task ExecuteAsync_MissingSkill_ReturnsOneWithoutThrowing()
    {
        // Server returns 404 for a skill that isn't published (regression test for #142).
        var handler = new QueuedHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = CreateClient(handler);

        var exitCode = await VersionsCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["no-such-skill"] },
            client);

        Assert.Equal(1, exitCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingSkill_ReturnsZeroAndListsVersions()
    {
        var versions = new[]
        {
            new SkillVersionSummary
            {
                Name = "example-skill",
                Version = "1.0.0",
                Sha256 = "abc123",
                PublishedAt = DateTimeOffset.UtcNow,
                IsLatest = true
            }
        };
        var json = JsonSerializer.Serialize(
            (IReadOnlyList<SkillVersionSummary>)versions,
            SkillServerClientJsonContext.Default.IReadOnlyListSkillVersionSummary);
        var handler = new QueuedHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        using var client = CreateClient(handler);

        var exitCode = await VersionsCommand.ExecuteAsync(
            new ParsedArgs { Positional = ["example-skill"] },
            client);

        Assert.Equal(0, exitCode);
    }

    private static SkillServerClient CreateClient(HttpMessageHandler handler)
    {
        return new SkillServerClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        });
    }

    private sealed class QueuedHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueuedHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri?.AbsolutePath ?? "");

            return Task.FromResult(_responses.Count == 0
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : _responses.Dequeue());
        }
    }
}
