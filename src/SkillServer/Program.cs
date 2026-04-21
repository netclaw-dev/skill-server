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

// Add controllers
builder.Services.AddControllers();

// Add services
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<BlobStorage>();
builder.Services.AddSingleton<SkillRepository>();
builder.Services.AddSingleton<IndexGenerator>();
builder.Services.AddSingleton<SkillUploadService>();

var app = builder.Build();

// Initialize database
var dbInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await dbInitializer.InitializeAsync();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();
