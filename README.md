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
| [Native Manifest](docs/specs/native-manifest.md) | HATEOAS-style sync feed for skills, sub-agents, and future native resources with API version negotiation. |
| [Security And Trust Model](docs/specs/security.md) | Trust boundaries, authentication, digest verification, and sync safety for native sync. |
| [Sub-Agent Sync Epic](docs/epics/subagent-sync.md) | Requirements and proposed GitHub issue breakdown for native manifest and sub-agent sync. |

## Quick Start

### Set the bootstrap key first

Generate and save a strong random secret in your password manager. Supply it before the first startup; **a database with no API keys leaves publishing, deletion, and key management unauthenticated**.

In a private Bash terminal, load the secret without putting it in shell history:

```bash
read -r -s -p "Bootstrap API key: " SKILLSERVER__APIKEY
printf '\n'
export SKILLSERVER__APIKEY
```

For automated deployments, inject the value through your deployment secret manager. Keep it out of committed Compose files and `.env` files.

### Docker

```bash
# The included Compose file maps SKILLSERVER_APIKEY to the server variable.
export SKILLSERVER_APIKEY="$SKILLSERVER__APIKEY"
docker compose -f docker/docker-compose.yml up -d
unset SKILLSERVER_APIKEY SKILLSERVER__APIKEY
```

### .NET

```bash
dotnet run --project src/SkillServer --urls http://localhost:8080
# After stopping the server:
unset SKILLSERVER__APIKEY
```

The server will start at `http://localhost:8080`. Reads and discovery remain unauthenticated; use a private network or proxy access policy for private content.

### Create a publishing key

Install the [host CLI](src/Netclaw.SkillServer.Cli/README.md#installation), then authenticate with the bootstrap key (or another existing valid key):

```bash
export SKILLSERVER_URL=http://localhost:8080
read -r -s -p "Existing SkillServer API key: " SKILLSERVER_API_KEY
printf '\n'
export SKILLSERVER_API_KEY
skillserver api-key list
skillserver api-key create --label ci-publish
unset SKILLSERVER_API_KEY
```

Run creation in a private terminal: it prints the new `sk-...` key once. Save the actual key immediately in your secret manager. The label is a name you choose; the key is generated and registered by the server. An arbitrary string saved in CI will not authenticate.

For GitHub Actions, save the returned key as the **`SKILLSERVER_API_KEY` secret** and pass it to the CLI:

```yaml
- name: Publish skills
  env:
    SKILLSERVER_URL: https://skills.example.com
    SKILLSERVER_API_KEY: ${{ secrets.SKILLSERVER_API_KEY }}
  run: skillserver publish-all ./skills
```

Create the secret under repository **Settings → Secrets and variables → Actions**, or under the deployment environment if the job uses one. See [GitHub secret setup](https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions).

Install the CLI and configure network access in preceding workflow steps. Avoid running key creation in CI logs. Every valid key can publish, delete, and manage other keys; a dedicated label helps rotation but does not restrict permissions. See [key management and rotation](#managing-keys).

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
| `GET /manifest.json` | Native manifest with API version negotiation; see [Native Manifest](docs/specs/native-manifest.md) |

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
| `POST /api/v1/api-keys` | Create a new API key 🔑 |
| `GET /api/v1/api-keys` | List all API keys (without secrets) 🔑 |
| `DELETE /api/v1/api-keys/{id}` | Revoke an API key 🔑 |

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

## Native Manifest

The native manifest (`/manifest.json`) is a HATEOAS-style discovery endpoint for SkillServer-aware clients. It provides API version negotiation and linked navigation to skills and sub-agents.

### Version Negotiation

The manifest declares available API versions. Clients negotiate to the most recent compatible version:

```json
{
  "apiVersion": "v1",
  "versions": {
    "v1": {
      "skills": { "href": "/skills/v1/index.json" },
      "subagents": { "href": "/subagents/v1/index.json" },
      "skillSearch": { "href": "/api/v1/skills" },
      "subagentSearch": { "href": "/api/v1/subagents" }
    }
  }
}
```

Clients follow links to traverse collections. The server owns pagination boundaries — clients never construct page URLs.

### Search

The manifest exposes search endpoints for discovery:

```text
/api/v1/skills?q={query}&skip={skip}&take={take}
/api/v1/subagents?q={query}&skip={skip}&take={take}
```

Search is the primary discovery mechanism. Full enumeration via the collection index is also supported.

### Client Library

The `NativeManifestClient` class provides version-aware manifest access:

```csharp
using var manifestClient = new NativeManifestClient("http://localhost:8080");

// Fetch manifest with automatic version negotiation
var manifest = await manifestClient.GetNativeManifestAsync();

// Resolve best supported version
var skillLinks = manifestClient.ResolveVersion(manifest, "v1");

// Browse skills
var skillIndex = await manifestClient.GetNativeSkillIndexAsync(skillLinks);
var skillDetail = await manifestClient.GetNativeSkillDetailAsync(skillLinks, "my-skill", "1.0.0");
```

## NetClaw Integration

Current NetClaw feed sync uses the RFC skill discovery endpoint. The native manifest provides richer skill metadata, sub-agent sync, and API version negotiation.

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

Follow [Quick Start](#set-the-bootstrap-key-first) to inject `SKILLSERVER__APIKEY` before first startup. The server stores its hash as the "bootstrap" key only if the database contains no keys. Changing that environment variable later does **not** rotate an existing key.

`SKILLSERVER__APIKEY` configures the server bootstrap. `SKILLSERVER_API_KEY` authenticates the host CLI or CI client. They are different variables.

### Managing Keys

Authenticate the host CLI with an existing valid key as shown in [Quick Start](#create-a-publishing-key), then:

```bash
skillserver api-key create --label ci-publish
skillserver api-key list
skillserver api-key delete 2
```

Creation returns a new secret once; listing returns IDs, labels, and dates, never the raw key or hash. `--expires-at <date>` optionally sets an expiration on creation. All valid keys have the same permissions, including key management.

To rotate a key, create a replacement, save it in your client or CI secret store, verify an authenticated command such as `skillserver api-key list` with the replacement, then delete the old key by its ID. No server restart or database reset is needed. The last remaining key cannot be deleted. If a newly created key is lost, create another using an existing valid key and revoke the lost key.

### Backwards Compatibility

When no API keys exist in the database, authentication is disabled and all endpoints are open. This preserves the original v1 behavior for existing deployments.

### Future Enhancements

- Rate limiting
- Audit logging

## License

Apache-2.0 - Copyright 2025 [Petabridge, LLC](https://petabridge.com)
