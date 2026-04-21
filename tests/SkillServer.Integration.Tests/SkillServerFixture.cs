using Microsoft.AspNetCore.Mvc.Testing;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

public sealed class SkillServerFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _httpClient;
    private SkillServerClient? _client;

    public HttpClient HttpClient => _httpClient
        ?? throw new InvalidOperationException("HttpClient not initialized");

    public SkillServerClient Client => _client
        ?? throw new InvalidOperationException("Client not initialized");

    public ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
        _client = new SkillServerClient(_httpClient);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}

[CollectionDefinition("SkillServer")]
public class SkillServerCollection : ICollectionFixture<SkillServerFixture>;
