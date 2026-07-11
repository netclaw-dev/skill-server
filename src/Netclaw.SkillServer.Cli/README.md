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

# Validate a sub-agent markdown file
skillserver lint subagent ./agents/support-agent.md

# Publish a sub-agent (version is a SkillServer packaging value)
skillserver publish-subagent ./agents/support-agent.md --version 1.0.0

# Re-publish a sub-agent if the version already exists
skillserver publish-subagent ./agents/support-agent.md --version 1.0.0 --force

# Dry run without uploading
skillserver publish-subagent ./agents/support-agent.md --version 1.0.0 --dry-run

# Download and verify a sub-agent to a caller-chosen path
skillserver download-subagent support-agent 1.0.0 ./staging/support-agent.md
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

# List all sub-agents
skillserver list-subagents

# Sub-agents as JSON
skillserver list-subagents --output json
```

### Verification

```bash
# Verify local files match the published version
skillserver verify ./my-skill
```

### Sub-Agent Files

Sub-agent files are single markdown files with YAML frontmatter and a prompt body.

```markdown
---
name: support-agent
description: Diagnose support issues and identify missing evidence.
modelRole: Main
timeoutSeconds: 120
visibility: internal
emitStructuredFindings: true
---

You are a support diagnostician.
```

The CLI validates the same required fields and value ranges as the server. The publication version is supplied with `--version` because local NetClaw sub-agent files do not require version metadata.

### Sync Primitives

The CLI and client library keep artifact sync destination-agnostic. SkillServer publishes verified `agent-md` artifacts; adapters for NetClaw, OpenCode, or other clients choose their own managed destination and format conversion policy. `download-subagent` writes only to the path you provide, verifies the manifest digest before moving the staged file into place, and refuses to overwrite existing files unless `--force` is set. Generic sync tooling should download and verify artifacts first, then hand the staged bytes to a target-specific adapter rather than hard-coding local application paths into the protocol.

### Deleting

```bash
# Delete a version (with confirmation prompt)
skillserver delete my-skill 1.0.0

# Skip confirmation
skillserver delete my-skill 1.0.0 --yes

# Delete a sub-agent version (with confirmation prompt)
skillserver delete-subagent support-agent 1.0.0

# Skip confirmation
skillserver delete-subagent support-agent 1.0.0 --yes
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
