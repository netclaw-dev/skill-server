# Netclaw.SkillClient

.NET client library for [SkillServer](https://github.com/netclaw-dev/skill-server), a self-hosted skill registry for AI agents.

SkillServer implements the [Cloudflare Agent Skills Discovery RFC v0.2.0](https://github.com/cloudflare/agent-skills-spec) and the [AgentSkills.io](https://agentskills.io) SKILL.md format.

## Installation

```bash
dotnet add package Netclaw.SkillClient
```

## Usage

### Direct Instantiation

```csharp
using Netclaw.SkillClient;

// Read-only access (no auth needed for discovery and downloads)
using var client = new SkillServerClient("http://localhost:8080");

// With API key for write operations (publish, delete)
using var client = new SkillServerClient("http://localhost:8080", apiKey: "sk-your-api-key");
```

### Dependency Injection

```csharp
// Read-only
services.AddSkillServerClient("http://localhost:8080");

// With API key
services.AddSkillServerClient("http://localhost:8080", "sk-your-api-key");
```

## Discovering Skills

```csharp
// Get the RFC-compliant skill index (/.well-known/agent-skills/index.json)
var index = await client.GetRfcIndexAsync();

foreach (var skill in index.Skills)
{
    Console.WriteLine($"{skill.Name} - {skill.Description}");
    Console.WriteLine($"  URL: {skill.Url}");
    Console.WriteLine($"  Digest: {skill.Digest}");
}
```

## Native Manifest Traversal

Use the native manifest when you need SkillServer-specific version history, resourceful skill artifacts, or sub-agent definitions.

```csharp
var manifest = await client.GetManifestAsync();

var skillIndex = await client.GetNativeSkillIndexAsync(manifest.Links.Skills);
var skillPage = await client.GetNativeSkillPageAsync(skillIndex.Pages[0]);
var skill = await client.GetNativeSkillIdentityAsync(skillPage.Items[0]);
var skillVersion = await client.GetNativeSkillVersionAsync(skill.Versions[0]);

var stagingPath = Path.GetTempFileName();
await using var staging = File.Create(stagingPath);
await client.DownloadNativeSkillArtifactAsync(skillVersion.Artifact, staging);
```

Sub-agents are native resources and do not appear in the RFC skill feed.

```csharp
var subAgentIndex = await client.GetNativeSubAgentIndexAsync(manifest.Links.SubAgents);
var subAgentPage = await client.GetNativeSubAgentPageAsync(subAgentIndex.Pages[0]);
var subAgent = await client.GetNativeSubAgentIdentityAsync(subAgentPage.Items[0]);
var subAgentVersion = await client.GetNativeSubAgentVersionAsync(subAgent.Versions[0]);

var stagingPath = Path.GetTempFileName();
await using var staging = File.Create(stagingPath);
await client.DownloadNativeSubAgentArtifactAsync(subAgentVersion, staging);
```

The download helpers verify SHA-256 digests before returning. They write to caller-provided streams, so your sync code chooses the staging path, final destination, and install policy.

## Listing and Browsing Skills

```csharp
// List all skills (paginated)
var skills = await client.ListSkillsAsync();
var page = await client.ListSkillsAsync(skip: 10, take: 5);

// Get all versions of a skill
var versions = await client.GetSkillVersionsAsync("my-skill");

// Get a specific version
var version = await client.GetVersionAsync("my-skill", "1.0.0");
```

## Searching Skills

```csharp
// Full-text search across skill names, descriptions, and categories
var results = await client.SearchSkillsAsync("kubernetes deployment");
var page = await client.SearchSkillsAsync("kubernetes", skip: 0, take: 10);
```

## Getting the Latest Version

```csharp
// Get the latest version of a specific skill
var latest = await client.GetLatestVersionAsync("my-skill");
Console.WriteLine($"Latest: {latest.Version} ({latest.Sha256})");
```

## Checking for Updates

```csharp
// Check if any of your cached skills have newer versions
var updates = await client.CheckUpdatesAsync([
    new CheckUpdateRequest { Name = "my-skill", Version = "1.0.0" },
    new CheckUpdateRequest { Name = "other-skill", Version = "2.1.0" }
]);

foreach (var item in updates.Where(u => u.HasUpdate))
{
    Console.WriteLine($"{item.Name}: {item.CurrentVersion} -> {item.LatestVersion}");
}
```

## Downloading Skills

```csharp
// Download SKILL.md as a string
var content = await client.GetSkillFileAsStringAsync("my-skill", "1.0.0");

// Download SKILL.md as a stream
await using var stream = await client.GetSkillFileAsync("my-skill", "1.0.0");

// Download a resource file from a skill archive
await using var resource = await client.GetSkillFileAsync("my-skill", "1.0.0", "prompts/system.md");

// Download a blob by its SHA-256 digest
await using var blob = await client.GetBlobAsync("sha256:abc123...");
```

## Publishing and Downloading Sub-Agents

Requires an API key for publishing and deleting. Reads are open on public registries.

```csharp
await using var file = File.OpenRead("agent.md");
var result = await client.UploadSubAgentAsync("support-agent", "1.0.0", file);

var versions = await client.GetSubAgentVersionsAsync("support-agent");
var agent = await client.GetSubAgentFileAsStringAsync("support-agent", "1.0.0");

await client.DeleteSubAgentVersionAsync("support-agent", "1.0.0");
```

## Adapter Guidance

`Netclaw.SkillClient` fetches manifests and verifies artifact bytes. It does not choose local filesystem paths or convert `agent-md` into a client-specific format.

Non-NetClaw clients should provide an adapter that:

- Chooses managed staging and install destinations.
- Downloads artifacts with `DownloadVerifiedArtifactAsync` or the native artifact helpers.
- Parses `agent-md` when the target client needs a different local agent format.
- Records source feed, resource name, version, digest, and generated local path for update and prune decisions.
- Writes only into adapter-owned managed locations so user-authored files are not overwritten.

## Verifying Integrity

```csharp
// Verify a downloaded skill matches its published digest
var isValid = await client.VerifyDigestAsync("my-skill", "1.0.0", "sha256:abc123...");
```

## Publishing Skills

Requires an API key with write access.

```csharp
await using var file = File.OpenRead("SKILL.md");
var result = await client.UploadSkillAsync("my-skill", "1.0.0", file, category: "coding");

Console.WriteLine($"Published: {result.Url}");
Console.WriteLine($"Digest: {result.Sha256}");
```

## Deleting Skills

```csharp
await client.DeleteVersionAsync("my-skill", "1.0.0");
```

## AOT Compatibility

This library is fully AOT-compatible. All JSON serialization uses source-generated `System.Text.Json` contexts with no runtime reflection.

## License

Apache-2.0 - Copyright 2025 [Petabridge, LLC](https://petabridge.com)
