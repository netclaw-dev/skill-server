# SkillServer

[![NuGet - Client](https://img.shields.io/nuget/v/Netclaw.SkillClient?label=Netclaw.SkillClient)](https://www.nuget.org/packages/Netclaw.SkillClient)
[![NuGet - CLI](https://img.shields.io/nuget/v/Netclaw.SkillServer.Cli?label=skillserver%20CLI)](https://www.nuget.org/packages/Netclaw.SkillServer.Cli)
[![GitHub Container](https://ghcr-badge.egpl.dev/netclaw-dev/skillserver/latest_tag?label=container)](https://ghcr.io/netclaw-dev/skillserver)

A self-hosted skill server for managing AI agent skills internally within organizations. Similar to self-hosted package registries (BaGet for NuGet, Verdaccio for npm, Docker Registry), SkillServer enables companies to:

- Host proprietary skills behind their firewall
- Control skill discovery and distribution
- Version skills with full history
- Integrate with NetClaw CLI and other AgentSkills.io-compatible agents

This repository contains three components:

| Component | Description | Install |
|-----------|-------------|---------|
| **SkillServer** | Self-hosted skill registry (web server) | `docker pull ghcr.io/netclaw-dev/skillserver` |
| **[skillserver CLI](src/Netclaw.SkillServer.Cli/README.md)** | Command-line tool for publishing and managing skills | `dotnet tool install -g Netclaw.SkillServer.Cli` |
| **[Netclaw.SkillClient](src/Netclaw.SkillClient/README.md)** | Typed .NET client library | `dotnet add package Netclaw.SkillClient` |

## Standards Support

SkillServer implements Agent Skills standards and defines native extensions for NetClaw-aware clients:

- **[AgentSkills.io](https://agentskills.io)** - The SKILL.md format standard (originally by Anthropic)
- **[Cloudflare Agent Skills Discovery RFC v0.2.0](https://github.com/cloudflare/agent-skills-discovery-rfc)** - Discovery via `/.well-known/agent-skills/index.json`
- **[SkillServer specifications](docs/specs/README.md)** - Native skill, sub-agent, and manifest sync specs

## Specifications

| Specification | Purpose |
|---------------|---------|
| [Skill Packages](docs/specs/skills.md) | How to author and publish AgentSkills.io-compatible SkillServer skills. |
| [Sub-Agent Packages](docs/specs/subagents.md) | How NetClaw sub-agent definitions are authored and how SkillServer will publish them. |
| [Native Manifest](docs/specs/native-manifest.md) | Planned non-RFC sync feed for skills, sub-agents, and future native resources. |
| [Sub-Agent Sync Epic](docs/epics/subagent-sync.md) | Requirements and proposed GitHub issue breakdown for native manifest and sub-agent sync. |

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
| `SKILLSERVER__APIKEY` | *(none)* | Initial API key, seeded on first run if no keys exist in DB |

## API Endpoints

### Discovery

| Endpoint | Description |
|----------|-------------|
| `GET /.well-known/agent-skills/index.json` | RFC-compliant skill index |
| `GET /manifest.json` | Planned NetClaw-native manifest; see [Native Manifest](docs/specs/native-manifest.md) |

### Skills

| Endpoint | Description |
|----------|-------------|
| `GET /skills` | List all skills |
| `GET /skills?q={query}` | Search skills (full-text) |
| `GET /skills/{name}` | Get skill info (all versions) |
| `GET /skills/{name}/latest` | Get latest version |
| `GET /skills/{name}/{version}` | Get specific version metadata |
| `GET /skills/{name}/{version}/SKILL.md` | Download SKILL.md |
| `GET /skills/{name}/{version}/{path}` | Download resource file |
| `POST /skills/check-updates` | Batch update check |
| `POST /skills` | Upload new skill version (multipart/form-data) 🔑 |
| `DELETE /skills/{name}/{version}` | Delete version 🔑 |

### Blobs

| Endpoint | Description |
|----------|-------------|
| `GET /blobs/sha256/{digest}` | Download blob by digest |
| `HEAD /blobs/sha256/{digest}` | Check if blob exists |

### API Keys

| Endpoint | Description |
|----------|-------------|
| `POST /api-keys` | Create a new API key 🔑 |
| `GET /api-keys` | List all API keys (without secrets) 🔑 |
| `DELETE /api-keys/{id}` | Revoke an API key 🔑 |

🔑 = Requires `Authorization: Bearer <key>` header (when API keys are configured)

### Health

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Health check |

## CLI Tool

The `skillserver` CLI is the recommended way to publish and manage skills.

### Install

```bash
# .NET global tool
dotnet tool install --global Netclaw.SkillServer.Cli

# Or standalone binary (Linux/macOS)
curl -fsSL https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.sh | bash

# Or standalone binary (Windows PowerShell)
iwr -useb https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.ps1 | iex
```

### Usage

```bash
# Configure
skillserver config init

# Publish a skill
skillserver publish ./my-skill

# Batch publish
skillserver publish-all ./skills

# List, search, verify, delete
skillserver list --search kubernetes
skillserver verify ./my-skill
skillserver delete my-skill 1.0.0 --yes
```

See the [CLI README](src/Netclaw.SkillServer.Cli/README.md) for the full command reference.

## Client Library

The `Netclaw.SkillClient` NuGet package provides a typed .NET client for SkillServer.

```bash
dotnet add package Netclaw.SkillClient
```

```csharp
using Netclaw.SkillClient;

using var client = new SkillServerClient("http://localhost:8080", apiKey: "sk-your-api-key");

// Full-text search
var results = await client.SearchSkillsAsync("kubernetes deployment");

// Get latest version of a skill
var latest = await client.GetLatestVersionAsync("my-skill");

// Batch update check
var updates = await client.CheckUpdatesAsync([
    new CheckUpdateRequest { Name = "my-skill", Version = "1.0.0" }
]);
```

See the [client library README](src/Netclaw.SkillClient/README.md) for full API documentation.

## NetClaw Integration

Current NetClaw feed sync uses the RFC skill discovery endpoint. The planned native manifest will add richer skill metadata and sub-agent sync.

Add SkillServer as a skill source using the configured feed URL for your NetClaw version:

```bash
netclaw skill source add my-server --feed http://localhost:8080
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

SkillServer uses **API key authentication** to protect write operations. Read and discovery endpoints remain open so agents can fetch skills without credentials.

### How It Works

- API keys are SHA-256 hashed before storage — raw keys are never persisted
- Keys are compared using constant-time comparison to prevent timing attacks
- Keys use the format `sk-{random}` (256 bits of entropy, base64url-encoded)
- Raw keys are shown **only once** at creation time and cannot be recovered

### Bootstrap

Set the `SKILLSERVER__APIKEY` environment variable before first run:

```bash
SKILLSERVER__APIKEY=sk-my-secret-key dotnet run --project src/SkillServer
```

The server hashes and stores this as a "bootstrap" key on first startup. Once any key exists in the database, the environment variable is ignored on subsequent starts.

### Managing Keys

All key management endpoints require an existing valid API key:

```bash
# Create a new key
curl -X POST http://localhost:8080/api-keys \
  -H "Authorization: Bearer sk-your-existing-key" \
  -H "Content-Type: application/json" \
  -d '{"label": "ci-deploy"}'

# List keys (never shows raw key or hash)
curl http://localhost:8080/api-keys \
  -H "Authorization: Bearer sk-your-existing-key"

# Revoke a key (cannot delete the last remaining key)
curl -X DELETE http://localhost:8080/api-keys/2 \
  -H "Authorization: Bearer sk-your-existing-key"
```

### Backwards Compatibility

When no API keys exist in the database, authentication is disabled and all endpoints are open. This preserves the original v1 behavior for existing deployments.

### Future Enhancements

- Rate limiting
- Audit logging

## License

Apache-2.0 - Copyright 2025 [Petabridge, LLC](https://petabridge.com)
