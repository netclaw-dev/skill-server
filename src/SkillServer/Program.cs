// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.StaticFiles;
using SkillServer;
using SkillServer.Data;
using SkillServer.Models;
using SkillServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization (AOT-compatible)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SkillServerJsonContext.Default);
});

// Add OpenAPI
builder.Services.AddOpenApi();

// Add services
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<BlobStorage>();
builder.Services.AddSingleton<SkillRepository>();
builder.Services.AddSingleton<SubAgentRepository>();
builder.Services.AddSingleton<ApiKeyRepository>();
builder.Services.AddSingleton<IndexGenerator>();
builder.Services.AddSingleton<NativeManifestGenerator>();
builder.Services.AddSingleton<SkillUploadService>();
builder.Services.AddSingleton<SubAgentUploadService>();
builder.Services.AddSingleton<SkillArchiveBackfillService>();
builder.Services.AddSingleton<ApiKeyService>();
builder.Services.AddSingleton<SeedDataService>();

var app = builder.Build();

// Initialize database
var dbInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await dbInitializer.InitializeAsync();

var archiveBackfillService = app.Services.GetRequiredService<SkillArchiveBackfillService>();
await archiveBackfillService.BackfillAsync();

// Seed skills and sub-agents from files
var seedService = app.Services.GetRequiredService<SeedDataService>();
await seedService.SeedAsync();

// Seed API key from environment variable
var apiKeyService = app.Services.GetRequiredService<ApiKeyService>();
await apiKeyService.SeedFromEnvironmentAsync();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map API endpoints (must be before fallback and static files)
app.MapSkillServerEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

// Serve CSR gallery shells for deep links.
app.MapFallbackToFile("/skills/{*path:nonfile}", "skills/index.html");
app.MapFallbackToFile("/subagents/{*path:nonfile}", "subagents/index.html");
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
