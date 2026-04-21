# SkillServer Tooling

## Development Tools

| Tool | Installation | Purpose |
|------|--------------|---------|
| .NET SDK 10.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com) | Build, test, publish |
| Incrementalist | `dotnet tool restore` | Release notes from commits |
| DocFX | `dotnet tool restore` | Documentation generation |

## Build Commands

```bash
# Restore tools (required first time)
dotnet tool restore

# Build
dotnet build -c Release

# Test
dotnet test -c Release

# Pack (only Netclaw.SkillClient)
dotnet pack -c Release -o ./bin/nuget

# Build container locally
dotnet publish src/SkillServer/SkillServer.csproj -c Release /t:PublishContainer

# Run locally
dotnet run --project src/SkillServer

# Run with Aspire (local dev)
dotnet run --project src/SkillServer.AppHost
```

## Scripts

| Script | Purpose |
|--------|---------|
| `build.ps1` | Generate release notes via Incrementalist |
| `scripts/Add-FileHeaders.ps1` | Add Petabridge copyright headers |
| `scripts/Add-FileHeaders.ps1 -Verify` | CI: Check all files have headers |
| `scripts/Add-FileHeaders.ps1 -WhatIf` | Preview which files need headers |
| `scripts/bumpVersion.ps1` | Bump version in Directory.Build.props |
| `scripts/getReleaseNotes.ps1` | Extract latest release notes |

## CI/CD (GitHub Actions)

### PR Validation (`pr_validation.yml`)
Triggers: Push to dev/main, PRs

| Job | Runs On | Purpose |
|-----|---------|---------|
| Test | ubuntu + windows | Build and test |
| NuGet Pack | ubuntu | Pack client library, upload artifacts |
| Docker Build | ubuntu | Validate container builds |

### Publish NuGet (`publish_nuget.yml`)
Triggers: Tags

Publishes `Netclaw.SkillClient` to NuGet.org.

### Publish Container (`publish_container.yml`)
Triggers: Tags

| Job | Purpose |
|-----|---------|
| build-x64 | Build linux-x64 image |
| build-arm64 | Build linux-arm64 image |
| create-manifest | Multi-arch manifest to GHCR |

## Container Registry

- **Registry:** `ghcr.io`
- **Image:** `ghcr.io/{owner}/skillserver`
- **Tags:** `latest`, `{version}`, `{version}-x64`, `{version}-arm64`

## Local Development

### Docker Compose
```bash
# Run pre-built image
docker compose -f docker/docker-compose.yml up -d

# Access at http://localhost:8080
```

### Environment Variables
| Variable | Default | Purpose |
|----------|---------|---------|
| `SKILLSERVER__DATAPATH` | `./data` | SQLite + blob storage path |
| `SKILLSERVER__BASEURL` | `http://localhost:8080` | Base URL for absolute links |

## IDE Configuration

- **ReSharper/Rider:** `SkillServer.slnx.DotSettings` - File header template
- File headers auto-inserted on new file creation in JetBrains IDEs
