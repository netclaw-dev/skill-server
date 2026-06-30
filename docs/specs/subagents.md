# Sub-Agent Package Specification

Status: current NetClaw file format, target SkillServer publication model.

This document defines the sub-agent definition format SkillServer should publish and the NetClaw runtime already understands. Sub-agents are not AgentSkills.io skills and must not be listed in the Cloudflare RFC skill feed.

## Goals

- Preserve the existing NetClaw sub-agent markdown format.
- Publish sub-agents as first-class native manifest resources.
- Keep sub-agent sync separate from skill artifact sync.
- Avoid a general dependency graph in the first version.
- Allow skills to route to sub-agents through existing `metadata.subagent` frontmatter.

## Local File Layout

NetClaw currently loads sub-agent definitions from one markdown file per agent:

```text
~/.netclaw/agents/
  technical-support-diagnostician.md
  code-analyst.md
  summarizer.md
```

The file has YAML frontmatter followed by a markdown system prompt body.

```markdown
---
name: technical-support-diagnostician
description: Diagnose incomplete technical support tickets and identify missing evidence.
modelRole: Compaction
timeoutSeconds: 120
visibility: user-facing
emitStructuredFindings: false
---

You are a technical support diagnostician.

Identify the most likely root cause, missing evidence, and next diagnostic step.
Return concise findings for the parent session.
```

The filename is for humans. The authoritative identity is `name` in frontmatter.

## Frontmatter

Current NetClaw-supported fields:

| Field | Required | Default | Meaning |
|-------|----------|---------|---------|
| `name` | Yes | None | Unique lowercase kebab-case name used by `spawn_agent`. |
| `description` | Yes | None | One-line description shown in the available sub-agents context layer. |
| `tools` | No | `[]` | Advisory tool metadata only. Does not grant or restrict runtime tools. |
| `modelRole` | No | `Compaction` | `Compaction` or `Main`. |
| `timeoutSeconds` | No | `60` | Inactivity timeout in seconds. Valid range: 5 to 600. |
| `prefillTimeoutSeconds` | No | Runtime default | Wait-for-first-delta timeout in seconds. Valid range: 5 to 3600. |
| `visibility` | No | `user-facing` | `user-facing` or `internal`. PascalCase values are also accepted. |
| `emitStructuredFindings` | No | `false` | Whether successful output becomes parent-reviewed memory findings. |

Unknown fields are ignored by the current NetClaw loader. SkillServer publication may read additional metadata without affecting NetClaw runtime behavior.

## Versioning

NetClaw local runtime does not require a sub-agent version today.

SkillServer publication does require a version for sync, caching, and rollback. The initial publication flow should accept a version out of band through the CLI, matching skill upload behavior. A future CLI may also accept a compatible frontmatter extension such as:

```yaml
metadata:
  version: "1.0.0"
```

Until NetClaw runtime needs local version awareness, version metadata should remain a SkillServer packaging concern.

## Runtime Semantics

Sub-agents are specialist workers spawned by a parent NetClaw session.

- The sub-agent system prompt is the markdown body after frontmatter.
- The first user message is the task supplied to `spawn_agent`.
- Optional runtime context is prefixed to that task, not written into the system prompt.
- Runtime tools are inherited from the parent session audience/profile policy.
- The sub-agent denylist prevents recursive `spawn_agent` calls.
- `tools` frontmatter is only advisory metadata.
- `user-facing` sub-agents are advertised to the frontline model.
- `internal` sub-agents are hidden from normal `spawn_agent` discovery.

## Relationship To Skills

A skill may route execution through one sub-agent by declaring `metadata.subagent` in `SKILL.md` frontmatter.

```yaml
---
name: support-triage
description: Triage support tickets and produce a concise diagnostic plan.
metadata:
  subagent: technical-support-diagnostician
---
```

When routed, the skill body is appended to the sub-agent prompt as a `Skill Overlay` during activation.

This is intentionally not a package dependency graph. The sync system should ensure referenced sub-agents can be installed, but runtime activation remains the source of truth for how a skill and sub-agent compose.

Sub-agents should not declare required skills in the first version. If a sub-agent needs a skill's instructions, encode that operational guidance in the sub-agent prompt or use a future NetClaw runtime field after there is a concrete loading requirement.

## SkillServer Artifact Shape

The native manifest should expose sub-agent versions with the same artifact semantics used for skills, but with sub-agent-specific `kind` and `type` values:

```json
{
  "kind": "subagent-version",
  "name": "technical-support-diagnostician",
  "version": "1.0.0",
  "type": "agent-md",
  "description": "Diagnose incomplete technical support tickets and identify missing evidence.",
  "url": "/subagents/technical-support-diagnostician/1.0.0/agent.md",
  "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
}
```

Field meanings:

| Field | Meaning |
|-------|---------|
| `kind` | Native manifest resource kind. Must be `subagent-version` for version details. |
| `name` | Sub-agent identity from frontmatter. |
| `version` | SkillServer publication version. |
| `type` | `agent-md` for a single markdown sub-agent definition. |
| `description` | Same description as frontmatter. |
| `url` | Artifact download URL. |
| `digest` | SHA-256 digest of the exact artifact bytes at `url`. |

Initial sub-agent sync should only support `agent-md`. Archive-based sub-agent packages can be added later if there is a real need for bundled resources.

## Validation Requirements

Sub-agent validation should reject:

- Missing or invalid YAML frontmatter.
- Missing `name` or `description`.
- Empty system prompt body.
- Invalid name format.
- Duplicate names within the same upload set or registry scope.
- Invalid `modelRole`, `visibility`, `timeoutSeconds`, or `prefillTimeoutSeconds` values.

Validation should warn, not fail, for unknown fields.

## Client Install Location

SkillServer does not define where sub-agent artifacts are installed after download. It publishes versioned `agent-md` artifacts and native manifest metadata; each client maps those artifacts into its own local configuration model.

NetClaw sync should install downloaded sub-agent definitions into a managed server-feed subdirectory under the NetClaw agents area rather than overwriting user-authored files directly.

The exact local path is a NetClaw implementation detail, not a SkillServer protocol requirement. NetClaw's layout should preserve these properties:

- User-authored `~/.netclaw/agents/*.md` files remain editable.
- Server-synced files are removable when the feed no longer advertises them.
- Duplicate names resolve deterministically with loud diagnostics.
- Sync state records version, digest, and source feed.

Non-NetClaw clients can use the same server artifacts by configuring their own sync destination. They should preserve equivalent ownership boundaries between user-authored files and server-synced files.

## Client Adapters

Non-NetClaw clients should sync sub-agents through an adapter that understands that client's local agent format.

The canonical SkillServer artifact is `agent-md`: YAML frontmatter plus a markdown system prompt body. An adapter may either install this file directly or translate it into the target client's native format.

Sub-agent sync should follow the same unopinionated model as skill sync. Shared tooling should fetch and verify artifacts, compare local state, and hand the result to a destination adapter. It should not assume NetClaw's `~/.netclaw/agents` layout, OpenCode's layout, Claude Code's layout, or any other target layout.

Adapter responsibilities:

- Decide whether the target client supports `agent-md` directly or needs conversion.
- Map `name`, `description`, and body text to the target client's required fields.
- Preserve advisory fields such as `tools`, `modelRole`, `timeoutSeconds`, and `visibility` when supported.
- Ignore unsupported advisory fields.
- Write generated files only into a configured managed destination for that target client.
- Track source feed, name, version, digest, and generated file path so updates and pruning do not touch user-authored files.
- Surface warnings when a target client cannot represent an important field.

Example adapter outcomes:

| Target Client Capability | Adapter Behavior |
|--------------------------|------------------|
| Accepts markdown agent files with frontmatter | Write `agent-md` directly. |
| Accepts markdown prompts but different frontmatter | Rewrite frontmatter and preserve the body. |
| Accepts JSON/YAML agent definitions | Convert frontmatter and body into the client's schema. |
| Does not support sub-agents | Ignore `subagent-version` resources and report unsupported capability if requested. |

This keeps SkillServer portable: it publishes one sub-agent artifact model, while clients own installation and format translation.
