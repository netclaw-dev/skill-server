# skillserver CLI

Command-line tool for publishing and managing skills on a [SkillServer](https://github.com/netclaw-dev/skill-server) instance.

## Installation

### .NET Global Tool

```bash
dotnet tool install --global Netclaw.SkillServer.Cli
```

### Standalone Binary (Linux/macOS)

```bash
curl -fsSL https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.sh | bash
```

### Standalone Binary (Windows)

```powershell
iwr -useb https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.ps1 | iex
```

Standalone binaries are self-contained and trimmed -- no .NET runtime required.

## Configuration

Configure your server URL and API key:

```bash
skillserver config init
```

Or set individually:

```bash
skillserver config set server-url https://skills.example.com
skillserver config set api-key sk-your-key
```

For CI/CD, use environment variables:

```bash
export SKILLSERVER_URL=https://skills.example.com
export SKILLSERVER_API_KEY=sk-your-key
```

Priority: CLI flags > environment variables > config file (`~/.skillserver/config.json`).

## Commands

### Publishing

```bash
# Publish a single skill (reads name/version from SKILL.md frontmatter)
skillserver publish ./my-skill

# Override version
skillserver publish ./my-skill --version 2.0.0-rc.1

# Re-publish (idempotent -- skips if version exists)
skillserver publish ./my-skill

# Force re-publish (delete + re-upload)
skillserver publish ./my-skill --force

# Batch publish all skills in a directory
skillserver publish-all ./skills

# Dry run
skillserver publish-all ./skills --dry-run
```

### Listing and Search

```bash
# List all skills
skillserver list

# Search
skillserver list --search akka

# List versions of a skill
skillserver versions my-skill

# JSON output
skillserver list --output json
```

### Verification

```bash
# Verify local files match the published version
skillserver verify ./my-skill
```

### Deleting

```bash
# Delete a version (with confirmation prompt)
skillserver delete my-skill 1.0.0

# Skip confirmation
skillserver delete my-skill 1.0.0 --yes
```

### API Key Management

```bash
skillserver api-key create --label "CI Pipeline"
skillserver api-key list
skillserver api-key delete 7
```

## Global Options

| Option | Description |
|--------|-------------|
| `--server-url <url>` | SkillServer URL (overrides config/env) |
| `--api-key <key>` | API key (overrides config/env) |
| `--output <text\|json>` | Output format (default: text) |
| `--verbose, -v` | Show HTTP request/response details |
| `--help, -h` | Show help |
| `--version` | Show version |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error |
| 2 | Partial failure (batch operations) |

## License

Apache-2.0 - Copyright 2025 [Petabridge, LLC](https://petabridge.com)
