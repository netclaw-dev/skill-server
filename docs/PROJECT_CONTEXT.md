# SkillServer Project Context

## Purpose

SkillServer is a self-hosted skill registry for AI agents, enabling organizations to host proprietary skills behind their firewall. It implements industry-standard discovery protocols for interoperability.

## Standards Implemented

| Standard | Endpoint | Purpose |
|----------|----------|---------|
| AgentSkills.io | SKILL.md format | Skill definition format |
| Cloudflare RFC v0.2.0 | `/.well-known/agent-skills/index.json` | Discovery protocol |
| NetClaw manifest | `/manifest.json` | NetClaw CLI compatibility |

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        SkillServer                          │
├─────────────────────────────────────────────────────────────┤
│  Minimal APIs (Endpoints.cs)                                │
│    ├── Discovery: RFC index, NetClaw manifest               │
│    ├── Skills: CRUD operations                              │
│    └── Blobs: Content-addressable storage                   │
├─────────────────────────────────────────────────────────────┤
│  Services                                                   │
│    ├── SkillUploadService - Upload handling, validation     │
│    ├── IndexGenerator - RFC/NetClaw index generation        │
│    └── BlobStorage - Content-addressable file storage       │
├─────────────────────────────────────────────────────────────┤
│  Data                                                       │
│    ├── SQLite (metadata, versions, file refs)               │
│    └── File system (sha256-addressed blobs)                 │
└─────────────────────────────────────────────────────────────┘
```

## Projects

| Project | Purpose | Packable |
|---------|---------|----------|
| `src/SkillServer` | ASP.NET Core server | No (container) |
| `src/Netclaw.SkillClient` | .NET client library | Yes (NuGet) |
| `src/SkillServer.AppHost` | Aspire local dev host | No |
| `tests/SkillServer.Tests` | Unit tests | No |
| `tests/SkillServer.Integration.Tests` | Integration tests | No |

## Tech Stack

- **.NET 10** - Target framework
- **ASP.NET Core Minimal APIs** - HTTP layer
- **SQLite + Dapper** - Metadata storage (AOT-compatible)
- **System.Text.Json** - Serialization (source-generated, AOT-ready)
- **Content-addressable storage** - Blob deduplication via SHA-256

## Design Constraints

1. **No authentication in v1** - Deploy behind firewall/reverse proxy
2. **AOT-ready** - No reflection-based serialization
3. **Single-file deployment** - SQLite, no external dependencies
4. **Multi-arch containers** - linux-x64, linux-arm64

## Ownership

- **Owner:** Petabridge, LLC
- **License:** Apache-2.0
- **Primary contributors:** Petabridge team (~80% of commits)
- **External contributions:** Welcome, follow contribution guidelines

## Current State (v0.1.0)

- Full CRUD for skills with versioning
- RFC and NetClaw discovery endpoints
- Content-addressable blob storage
- 52 tests passing (45 unit + 7 integration)
- Multi-arch container publishing
- NuGet client library

## Future Roadmap

- API key authentication
- Rate limiting
- Audit logging
- Webhook notifications
