# Skill Package Specification

Status: active for skill authoring, partial implementation for artifact distribution alignment.

This document defines how SkillServer treats skills. The authoring format remains compatible with AgentSkills.io. SkillServer-specific behavior must live in explicit extension fields or native manifest metadata, not in a competing skill format.

## Goals

- Keep `SKILL.md` compatible with AgentSkills.io.
- Keep `/.well-known/agent-skills/index.json` compatible with the Cloudflare Agent Skills Discovery RFC.
- Reuse the RFC skill artifact shape inside the richer native manifest.
- Preserve the existing NetClaw `metadata.subagent` routing convention.
- Avoid defining a package-manager dependency graph for skills and sub-agents in the first version.

## Directory Layout

A directory-based skill contains a required `SKILL.md` file and optional supporting resources:

```text
my-skill/
  SKILL.md
  references/
    guide.md
  scripts/
    helper.sh
  assets/
    template.json
```

The required `SKILL.md` file has YAML frontmatter followed by Markdown instructions.

## Frontmatter

SkillServer accepts the AgentSkills.io fields below.

| Field | Required | Notes |
|-------|----------|-------|
| `name` | Yes | Lowercase kebab-case skill identity. Must match the parent directory for strict validation. |
| `description` | Yes | Short activation description. Maximum 1024 characters. |
| `license` | No | License name or bundled license reference. |
| `compatibility` | No | Runtime or client compatibility notes. |
| `allowed-tools` | No | Experimental AgentSkills.io advisory field. |
| `metadata` | No | Extension point for string or scalar values. |

SkillServer and NetClaw currently use these metadata conventions:

| Metadata Key | Status | Meaning |
|--------------|--------|---------|
| `version` | Implemented in NetClaw scanning | Optional local version hint. SkillServer upload currently receives version out of band. |
| `subagent` | Implemented in NetClaw | Route this skill through a named user-facing sub-agent. |

Example:

```yaml
---
name: support-triage
description: Triage support tickets and produce a concise diagnostic plan.
metadata:
  version: "1.0.0"
  subagent: technical-support-diagnostician
---
```

## Sub-Agent Routing

`metadata.subagent` is a NetClaw execution route, not a package dependency declaration.

When a skill declares `metadata.subagent`:

- The value must be a non-empty lowercase kebab-case sub-agent name.
- The target sub-agent must be registered and `user-facing` at runtime.
- NetClaw routes slash-command and `skill_load` activation through that sub-agent.
- The skill body is appended to the sub-agent system prompt as a `Skill Overlay`.
- The sub-agent's runtime tools are still inherited from the parent session policy.

This keeps the relationship simple:

```text
skill activation -> routed through one sub-agent
```

It does not mean:

```text
skill package -> installs sub-agent package as a transitive dependency
```

## RFC Artifact Shape

Every published skill version should have a canonical artifact object matching the Cloudflare RFC fields plus SkillServer's version extension:

```json
{
  "name": "support-triage",
  "version": "1.0.0",
  "type": "skill-md",
  "description": "Triage support tickets and produce a concise diagnostic plan.",
  "url": "/skills/support-triage/1.0.0/SKILL.md",
  "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
}
```

Field meanings:

| Field | Meaning |
|-------|---------|
| `name` | Skill identity from `SKILL.md`. |
| `version` | SkillServer version for this artifact. |
| `type` | `skill-md` for a single `SKILL.md` artifact or `archive` for a packaged skill directory. |
| `description` | Same activation description as `SKILL.md`. |
| `url` | Artifact download URL. |
| `digest` | SHA-256 digest of the exact artifact bytes at `url`, formatted as `sha256:{hex}`. |

The native manifest must reuse this object instead of inventing another skill install contract.

## Resource Distribution

Target behavior:

- Skills with only `SKILL.md` should publish as `type: "skill-md"`.
- Skills with supporting files should publish as `type: "archive"`.
- Archive artifacts should contain `SKILL.md` at the archive root plus supporting files under their relative paths.
- The RFC feed and native manifest should point to the same artifact URL and digest for a given skill version.

Current implementation gap:

- SkillServer currently stores resource files individually and exposes them through a `resources` extension in the RFC-shaped index.
- SkillServer has an `archive` type constant but does not currently build or serve skill archives.

The migration path is to add archive artifact support while preserving existing per-file resource routes for compatibility.

## Validation Requirements

Skill validation should reject:

- Missing or invalid YAML frontmatter.
- Missing `name` or `description`.
- Names that do not meet AgentSkills.io naming rules.
- Directory/name mismatches in strict mode.
- Resource paths with path traversal or absolute paths.
- Invalid `metadata.subagent` values when present.

Validation should warn, not fail, for unknown metadata keys.

## Native Manifest Projection

The native manifest version detail should wrap the same RFC artifact object and add SkillServer metadata around it:

```json
{
  "kind": "skill-version",
  "artifact": {
    "name": "support-triage",
    "version": "1.0.0",
    "type": "skill-md",
    "description": "Triage support tickets and produce a concise diagnostic plan.",
    "url": "/skills/support-triage/1.0.0/SKILL.md",
    "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
  },
  "routesToSubagent": {
    "name": "technical-support-diagnostician",
    "href": "/manifest/subagents/technical-support-diagnostician/index.json"
  },
  "links": {
    "self": "/manifest/skills/support-triage/versions/1.0.0.json"
  }
}
```

The manifest may expose parsed `metadata.subagent` as `routesToSubagent`, but the source of truth remains the skill frontmatter.
