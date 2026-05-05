// -----------------------------------------------------------------------
// <copyright file="SkillServerFixture.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.Mvc.Testing;
using Netclaw.SkillClient;
using Xunit;

namespace SkillServer.Integration.Tests;

public sealed class SkillServerFixture : IAsyncLifetime
{
    public const string TestApiKey = "sk-test-integration-key-12345";

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _httpClient;
    private HttpClient? _authHttpClient;
    private SkillServerClient? _client;
    private SkillServerClient? _authClient;

    public HttpClient HttpClient => _httpClient
        ?? throw new InvalidOperationException("HttpClient not initialized");

    public HttpClient AuthenticatedHttpClient => _authHttpClient
        ?? throw new InvalidOperationException("Authenticated HttpClient not initialized");

    public SkillServerClient Client => _client
        ?? throw new InvalidOperationException("Client not initialized");

    public SkillServerClient AuthenticatedClient => _authClient
        ?? throw new InvalidOperationException("AuthenticatedClient not initialized");

    public ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();

        _authHttpClient = _factory.CreateClient();
        _authHttpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestApiKey);

        _client = new SkillServerClient(_httpClient);
        _authClient = new SkillServerClient(_authHttpClient);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _authHttpClient?.Dispose();
        _client?.Dispose();
        _authClient?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}

[CollectionDefinition("SkillServer")]
public class SkillServerCollection : ICollectionFixture<SkillServerFixture>;
