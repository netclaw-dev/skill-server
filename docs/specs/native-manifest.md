# Native Manifest Specification

Status: Implemented. `/manifest.json` is the root discovery endpoint for SkillServer-aware clients.

The native manifest is SkillServer's versioned, HATEOAS-style sync feed. It must not replace the Cloudflare Agent Skills Discovery RFC feed (`/.well-known/agent-skills/index.json`). Instead, it provides a linked catalog for SkillServer-aware clients that need version negotiation, search, version history, sub-agent definitions, and additional metadata.

## Goals

- Add `/manifest.json` as a native entry point for SkillServer-aware clients.
- Use HATEOAS-style links with versioned path prefixes.
- Support API version negotiation: client picks the most recent version it supports.
- Expose search endpoints for discovery without full enumeration.
- Publish sub-agents as first-class native resources.
- Let clients follow links instead of constructing URLs.

## Non-Goals

- Do not list sub-agents in the Cloudflare RFC skill feed.
- Do not include the RFC feed in the native manifest (it exists independently).
- Do not create a separate skill install format for native clients.
- Do not define a general transitive dependency resolver in the first version.

## Version Negotiation

The manifest declares which API versions are available. Clients should:

1. Know which versions they support (e.g., `["v1"]`).
2. Fetch `/manifest.json`.
3. Find the most recent version from the intersection of client-supported and server-available versions.
4. Use that version's links for all subsequent requests.
5. If no supported version is found, throw `NotSupportedException` or fall back to the oldest available version.

When a new API version ships, older clients continue to work against their supported version. New clients automatically use the newest version they understand.

## Endpoint Shape

The stable entry point is:

```text
GET /manifest.json
```

The manifest tree uses versioned path prefixes for collection and page navigation:

```text
/manifest.json
/skills/v1/index.json
/skills/v1/pages/all.json
/skills/v1/{skillName}/index.json
/skills/v1/{skillName}/versions/{version}.json
/subagents/v1/index.json
/subagents/v1/pages/all.json
/subagents/v1/{subagentName}/index.json
/subagents/v1/{subagentName}/versions/{version}.json
```

Clients must follow returned `href` values. The path structure is readable and stable, but the server owns pagination boundaries.

## Root Manifest

`/manifest.json` links to the native collections exposed by the registry, organized by API version.

```json
{
  "$schema": "https://schemas.netclaw.dev/skillserver/manifest/0.1.0",
  "apiVersion": "v1",
  "versions": {
    "v1": {
      "self": { "href": "/manifest.json" },
      "skills": { "href": "/skills/v1/index.json" },
      "subagents": { "href": "/subagents/v1/index.json" },
      "skillSearch": { "href": "/api/v1/skills" },
      "subagentSearch": { "href": "/api/v1/subagents" }
    }
  }
}
```

The `apiVersion` field indicates the currently recommended version. The `versions` object contains all available versions, keyed by version tag. When v2 ships, a `"v2": { ... }` entry is added.

The RFC feed (`/.well-known/agent-skills/index.json`) is a separate standard and is not included in the manifest.

## Collection Index

A collection index links to one or more server-defined pages.

```json
{
  "kind": "skill-index",
  "links": {
    "self": { "href": "/skills/v1/index.json" }
  },
  "pages": [
    {
      "range": "all",
      "href": "/skills/v1/pages/all.json"
    }
  ]
}
```

The `range` value is informational. The server may replace a single "all" page with multiple pages in the future without breaking clients.

## Collection Page

A collection page lists identities and coarse version bounds.

```json
{
  "kind": "skill-page",
  "range": "all",
  "items": [
    {
      "name": "support-triage",
      "latestVersion": "1.2.0",
      "versionRange": {
        "min": "1.0.0",
        "max": "1.2.0",
        "count": 4
      },
      "href": "/skills/v1/support-triage/index.json"
    }
  ],
  "links": {
    "self": { "href": "/skills/v1/pages/all.json" }
  }
}
```

If a page grows too large, the server may replace it with links to smaller child pages in a future schema version.

## Identity Index

An identity index lists all versions for one skill or sub-agent.

```json
{
  "kind": "skill",
  "name": "support-triage",
  "latestVersion": "1.2.0",
  "versions": [
    {
      "version": "1.2.0",
      "publishedAt": "2026-06-29T12:00:00Z",
      "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
      "href": "/skills/v1/support-triage/versions/1.2.0.json"
    }
  ],
  "links": {
    "self": { "href": "/skills/v1/support-triage/index.json" }
  }
}
```

## Skill Version Detail

Skill version details wrap the exact artifact object that the RFC feed exposes for the same skill version.
Resourceful skill artifacts use SkillServer's canonical `.zip` archive route, `/api/v1/skills/{skillName}/{version}/archive.zip`.

```json
{
  "kind": "skill-version",
  "artifact": {
    "name": "support-triage",
    "version": "1.2.0",
    "type": "archive",
    "description": "Triage support tickets and produce a concise diagnostic plan.",
    "url": "/api/v1/skills/support-triage/1.2.0/archive.zip",
    "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
  },
  "routesToSubagent": {
    "name": "technical-support-diagnostician",
    "href": "/subagents/v1/technical-support-diagnostician/index.json"
  },
  "links": {
    "self": { "href": "/skills/v1/support-triage/versions/1.2.0.json" }
  }
}
```

The `routesToSubagent` value is derived from `metadata.subagent` in `SKILL.md`. It is a runtime route hint, not a package dependency.

## Sub-Agent Version Detail

Sub-agent version details use the same content-addressed artifact semantics, but are native-only resources.

```json
{
  "kind": "subagent-version",
  "name": "technical-support-diagnostician",
  "version": "1.0.0",
  "type": "agent-md",
  "description": "Diagnose incomplete technical support tickets and identify missing evidence.",
  "url": "/api/v1/subagents/technical-support-diagnostician/1.0.0/agent.md",
  "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
  "links": {
    "self": { "href": "/subagents/v1/technical-support-diagnostician/versions/1.0.0.json" }
  }
}
```

## Search

The manifest exposes search endpoints that clients can use for discovery without full enumeration:

```json
{
  "skillSearch": { "href": "/api/v1/skills" },
  "subagentSearch": { "href": "/api/v1/subagents" }
}
```

Clients append query parameters:
- `?q={query}` — full-text search
- `?skip={skip}&take={take}` — pagination

The search endpoint is the primary discovery mechanism. Full enumeration via the collection index is supported but may be slower for large registries.

## Sync Semantics

Native clients should:

1. Fetch `/manifest.json`.
2. Resolve the best supported API version.
3. Follow collection links for supported resource kinds.
4. Follow page links to identity indexes.
5. Compare version and digest with local sync state.
6. Download changed artifacts from `url`.
7. Verify `digest` before installing any artifact.
8. Keep existing local artifacts when a network request fails.
9. Prune server-synced artifacts only after a successful collection sync confirms removal.

Clients must ignore unsupported resource kinds and unknown fields.

## Client-Controlled Destinations

The native manifest describes what artifacts exist and where to download them. It does not prescribe where a client must install them.

Each client owns its local sync policy:

- NetClaw may install server-synced skills and sub-agents under managed directories inside its configured home directory.
- A non-NetClaw client may sync the same artifacts into its own skills, commands, agents, plugins, or cache directories.
- A client may expose configuration for per-feed destination roots, per-resource-kind destination roots, or both.
- A client must keep enough local state to avoid overwriting user-authored files and to prune only artifacts it previously synced from that feed.

Manifest `url`, `digest`, `name`, `version`, `kind`, and `type` fields are portable. Local filesystem layout is intentionally out of scope for the server protocol.

## Non-NetClaw Client Sync

Non-NetClaw clients should consume the native manifest through an adapter layer rather than requiring SkillServer to publish client-specific feeds.

The recommended split is:

- SkillServer publishes canonical, verified artifacts.
- `Netclaw.SkillClient` fetches manifests and artifact bytes without choosing local paths or target client formats.
- CLI sync commands provide unopinionated download/update primitives for supported resource kinds.
- A client-specific adapter maps each supported artifact into that client's local format and destination.

This is the same contract SkillServer should use for skills and sub-agents: sync tooling can fetch, verify, compare, and stage artifacts, but the consuming client decides how those artifacts become local runtime configuration.

For sub-agents, an adapter should implement this flow:

1. Follow the `subagents` collection from `/manifest.json`.
2. Traverse to each `subagent-version` detail.
3. Download the `agent-md` artifact from `url`.
4. Verify the artifact bytes against `digest`.
5. Parse the markdown frontmatter and body.
6. Convert the definition to the target client's local agent format.
7. Write only into the adapter's configured managed sync location.
8. Record source feed, name, version, digest, and generated local file path for pruning and updates.

If the target client can consume NetClaw-style markdown directly, the adapter can install `agent-md` without conversion. If the target client has a different format, the adapter owns the mapping.

Minimum portable fields for adapters are:

| Source Field | Adapter Use |
|--------------|-------------|
| `name` | Local agent identity or filename. |
| `description` | Discovery text shown by the target client. |
| Markdown body | System prompt or agent instructions. |
| `tools` | Advisory capability metadata when the target client supports it. |
| `modelRole` | Optional model/profile hint when the target client supports it. |
| `timeoutSeconds` | Optional execution timeout when the target client supports it. |
| `visibility` | Whether the adapter exposes or hides the agent when the target client supports visibility. |

Adapters must ignore unsupported fields rather than rejecting otherwise valid artifacts. A target client that needs additional metadata should use namespaced extension fields and document how its adapter interprets them.

The server protocol should remain one manifest with portable artifact types. It should not grow separate `/manifest/opencode`, `/manifest/claude-code`, or similar feeds unless a client has a hard incompatibility that cannot be solved by an adapter.

The first-party CLI and library should make OpenCode, Claude Code, and other future consumers supportable by composition rather than hard-coding their paths into the server protocol. A future OpenCode sync command, for example, should be able to use the same manifest traversal and verified artifact download helpers, then apply an OpenCode-specific destination and format adapter.

## Authentication

Read access may be open for public registries. Private registries may require the same bearer API key already used by SkillServer write endpoints and NetClaw feed configuration.

Clients should send configured credentials to both manifest and artifact URLs for the same feed origin.

See the [Security And Trust Model](security.md) for the full trust boundaries, digest-verification requirements, archive-extraction responsibilities, and sync-safety behavior that apply to native sync.

## Compatibility With The RFC Feed

The RFC feed remains the compatibility contract for generic AgentSkills.io clients.

The native manifest must not change the meaning of RFC fields:

- `name` identifies the same skill.
- `description` has the same activation meaning.
- `type` has the same artifact distribution meaning.
- `url` points to the same bytes for a version.
- `digest` verifies the same bytes at `url`.

For sub-agents, native resources should mimic those artifact semantics without pretending to be RFC skills.
