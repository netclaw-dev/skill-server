# SkillServer

A self-hosted skill server for managing AI agent skills internally within organizations. Similar to self-hosted package registries (BaGet for NuGet, Verdaccio for npm, Docker Registry), SkillServer enables companies to:

- Host proprietary skills behind their firewall
- Control skill discovery and distribution
- Version skills with full history
- Integrate with NetClaw CLI and other AgentSkills.io-compatible agents

## Standards Support

SkillServer implements two complementary standards:

- **AgentSkills.io** - The SKILL.md format standard (originally by Anthropic)
- **Cloudflare Agent Skills Discovery RFC v0.2.0** - Discovery via `/.well-known/agent-skills/index.json`
- **NetClaw manifest.json** - Backwards compatibility with existing NetClaw infrastructure

## Quick Start

### Docker

```bash
docker compose -f docker/docker-compose.yml up -d
```

### .NET

```bash
dotnet run --project src/SkillServer
```

The server will start at `http://localhost:8080`.

## Configuration

Configuration is via environment variables or `appsettings.json`:

| Variable | Default | Description |
|----------|---------|-------------|
| `SKILLSERVER__DATAPATH` | `./data` | Directory for SQLite database and blobs |
| `SKILLSERVER__BASEURL` | `http://localhost:8080` | Base URL for generating absolute URLs in indexes |

## API Endpoints

### Discovery

| Endpoint | Description |
|----------|-------------|
| `GET /.well-known/agent-skills/index.json` | RFC-compliant skill index |
| `GET /manifest.json` | NetClaw-compatible manifest |

### Skills

| Endpoint | Description |
|----------|-------------|
| `GET /skills` | List all skills |
| `GET /skills/{name}` | Get skill info (all versions) |
| `GET /skills/{name}/{version}` | Get specific version metadata |
| `GET /skills/{name}/{version}/SKILL.md` | Download SKILL.md |
| `GET /skills/{name}/{version}/{path}` | Download resource file |
| `POST /skills` | Upload new skill version (multipart/form-data) |
| `DELETE /skills/{name}/{version}` | Delete version |

### Blobs

| Endpoint | Description |
|----------|-------------|
| `GET /blobs/sha256/{digest}` | Download blob by digest |
| `HEAD /blobs/sha256/{digest}` | Check if blob exists |

### Health

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Health check |

## Uploading Skills

Upload a SKILL.md file:

```bash
curl -X POST http://localhost:8080/skills \
  -F "name=my-skill" \
  -F "version=1.0.0" \
  -F "category=internal" \
  -F "file=@SKILL.md"
```

## Client Library

Install the client library:

```bash
dotnet add package SkillServer.Client
```

Usage:

```csharp
using SkillServer.Client;

// Direct instantiation
using var client = new SkillServerClient("http://localhost:8080");

// Or via DI
services.AddSkillServerClient("http://localhost:8080");

// Get RFC index
var index = await client.GetRfcIndexAsync();

// Get NetClaw manifest
var manifest = await client.GetNetclawManifestAsync();

// Download a skill
var content = await client.GetSkillFileAsStringAsync("my-skill", "1.0.0");

// Verify digest
var isValid = await client.VerifyDigestAsync("my-skill", "1.0.0", "sha256:...");
```

## NetClaw Integration

Add SkillServer as a skill source:

```bash
netclaw skill source add my-server --feed http://localhost:8080/manifest.json
```

## Development

### Prerequisites

- .NET 10 SDK

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Container Publishing

The project uses .NET's built-in container publishing:

```bash
dotnet publish src/SkillServer -c Release /t:PublishContainer
```

## Architecture

- **SQLite** for metadata (skill names, versions, file references)
- **File system** for blobs (content-addressable storage using SHA-256)
- **Dapper** for database access (AOT-compatible)
- **System.Text.Json** with source generators (AOT-compatible)

## Security

For v1, authentication is **not built in**. Deploy behind your firewall or reverse proxy with authentication.

Future versions will add:
- API key authentication
- Rate limiting
- Audit logging

## License

Apache-2.0
