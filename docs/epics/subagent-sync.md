# Epic: Native Manifest And Sub-Agent Sync

Status: planning.

This epic delivers a SkillServer-native sync model for skills and NetClaw sub-agent definitions while keeping the Cloudflare Agent Skills Discovery RFC feed compatible.

GitHub tracking:

- Epic: [#84](https://github.com/netclaw-dev/skill-server/issues/84)
- Specs: [#85](https://github.com/netclaw-dev/skill-server/issues/85)
- RFC artifact alignment: [#86](https://github.com/netclaw-dev/skill-server/issues/86)
- Native manifest skills collection: [#87](https://github.com/netclaw-dev/skill-server/issues/87)
- Sub-agent storage and artifact endpoints: [#88](https://github.com/netclaw-dev/skill-server/issues/88)
- Sub-agent manifest collection: [#89](https://github.com/netclaw-dev/skill-server/issues/89)
- Client support: [#90](https://github.com/netclaw-dev/skill-server/issues/90)
- CLI support: [#91](https://github.com/netclaw-dev/skill-server/issues/91)
- NetClaw sync integration: [#92](https://github.com/netclaw-dev/skill-server/issues/92)
- Security and compatibility hardening: [#93](https://github.com/netclaw-dev/skill-server/issues/93)

## Problem

SkillServer currently publishes one implemented discovery document: `/.well-known/agent-skills/index.json`. It also exposes first-party `/skills` APIs, but there is no implemented `/manifest.json` native feed despite README references.

NetClaw already has a sub-agent format and skill-to-sub-agent routing through `metadata.subagent`. SkillServer cannot publish or sync those sub-agent definitions today.

## Product Requirements

- Generic AgentSkills.io clients can continue using the RFC feed without understanding SkillServer extensions.
- NetClaw-aware clients can sync skills and sub-agent definitions from one native entry point.
- Skill version details in the native feed reuse the same artifact object exposed by the RFC feed.
- Sub-agents are first-class native resources, not fake skills.
- Skill-to-sub-agent binding uses existing `metadata.subagent` frontmatter.
- The native manifest uses linked path-based pagination instead of query-string cursor contracts.
- CLI tooling can validate sub-agent definitions before publication.
- Sync is safe: digest-verified, failure-tolerant, and non-destructive to user-authored local files.
- Sync destinations are client-controlled; the server manifest never prescribes local filesystem paths.
- Non-NetClaw clients consume sub-agents through adapters that map portable `agent-md` artifacts into their own local formats.
- CLI and client-library sync primitives are destination-agnostic for both skills and sub-agents.

## Non-Goals

- No general package dependency graph in the first release.
- No sub-agent entries in the Cloudflare RFC skill feed.
- No archive-based sub-agent packages until there is a concrete resource bundling need.
- No automatic context loading of every related skill or sub-agent.

## Delivery Phases

### Phase 1: Specifications And Compatibility Rules

Define the public contracts before implementation.

Deliverables:

- Skill package spec.
- Sub-agent package spec.
- Native manifest spec.
- README links.
- Documented current implementation gaps.

Acceptance criteria:

- Specs describe how to author skills and sub-agents.
- Specs state that `metadata.subagent` is a route, not a dependency.
- Specs state that native skill details reuse the RFC artifact shape.
- Specs state that `/manifest.json` is planned until implemented.

### Phase 2: RFC Artifact Alignment For Skills

Bring SkillServer's skill artifact model closer to the Cloudflare RFC.

Deliverables:

- Archive artifact generation for skills with resources.
- Archive download endpoint for versioned skills.
- RFC index emits `type: "archive"` for resourceful skills.
- Existing per-file resource routes remain available for compatibility.

Acceptance criteria:

- `skill-md` entries digest the raw `SKILL.md` bytes.
- `archive` entries digest the raw archive bytes.
- Archives contain `SKILL.md` at the root.
- Archive paths reject traversal and absolute paths.
- Existing resource tests keep passing or have explicit compatibility replacements.

### Phase 3: Native Manifest API

Implement the linked native feed.

Deliverables:

- `GET /manifest.json` root document.
- Skills collection index and pages.
- Skill identity index and version detail endpoints.
- Source-generated JSON models.
- Integration tests for link traversal.

Acceptance criteria:

- Clients can discover skill identities without using query strings.
- Clients can follow links from root to version detail.
- Skill version detail embeds the same artifact fields as the RFC feed.
- Unknown future collections can be ignored by older clients.

### Phase 4: Sub-Agent Registry Support

Add sub-agent storage, upload, download, and native manifest projection.

Deliverables:

- Sub-agent domain models and SQLite schema.
- Markdown parser/validator matching NetClaw's current format.
- `POST /subagents` upload endpoint.
- `GET /subagents/{name}/{version}/agent.md` artifact endpoint.
- Delete/version metadata endpoints as needed.
- Native manifest sub-agent collection, identity, and version detail endpoints.

Acceptance criteria:

- Invalid sub-agent markdown is rejected with actionable errors.
- Duplicate sub-agent versions conflict deterministically.
- Downloads verify against manifest digests.
- Sub-agents never appear in the RFC skill feed.

### Phase 5: CLI Support

Extend `skillserver` CLI to work with sub-agents.

Deliverables:

- `skillserver validate subagent <path>` or equivalent lint command.
- `skillserver publish-subagent <path>` command.
- `publish-all` support for mixed skill/sub-agent source trees or a separate sub-agent mode.
- Helpful diagnostics for invalid frontmatter.

Acceptance criteria:

- CLI validates the same fields as the server.
- CLI can publish a single `.md` sub-agent definition.
- CLI can avoid duplicate uploads unless forced.
- CLI docs include sub-agent examples.

### Phase 6: Client And NetClaw Sync Integration

Teach clients to consume the native manifest.

Deliverables:

- `Netclaw.SkillClient` methods for native manifest traversal.
- Sub-agent upload/download client methods.
- Destination-agnostic sync primitives for verified artifact download.
- Adapter guidance for non-NetClaw clients that need to map `agent-md` into a different local agent format.
- CLI sync behavior for sub-agents that mirrors the unopinionated skill sync model.
- NetClaw daemon sync support for native manifest sub-agent resources.
- Local sync state for sub-agents.
- Safe install path for server-synced sub-agent files.

Acceptance criteria:

- NetClaw can sync sub-agents from SkillServer without overwriting user-authored files.
- Non-NetClaw clients can reuse the library and provide their own destination/format adapters.
- OpenCode or other future clients can be supported without changing the server manifest shape.
- NetClaw can resolve skill `metadata.subagent` targets after sync.
- Failed sync keeps existing local sub-agents.
- Removed server-side sub-agents are pruned only after a confirmed successful feed sync.

### Phase 7: Hardening, Docs, And Release Readiness

Make the feature safe enough to ship.

Deliverables:

- Security review for archive extraction and markdown prompt distribution.
- Integration tests across server, CLI, and client.
- Migration notes for existing resourceful skill feeds.
- Updated README and CLI documentation.
- Release notes.

Acceptance criteria:

- `dotnet build -c Release` succeeds.
- `dotnet test -c Release` passes.
- Header verification passes.
- Slopwatch passes with no new violations.
- Docs clearly separate RFC compatibility from native manifest behavior.

## GitHub Issues

### Issue 1: Define native manifest and sub-agent sync specifications

Labels: `documentation`, `spec`, `epic`

Body:

```markdown
Create the public design specs for SkillServer native sync.

Tasks:
- [ ] Define AgentSkills.io-compatible skill package rules.
- [ ] Define NetClaw sub-agent package rules.
- [ ] Define `/manifest.json` native feed structure.
- [ ] Document `metadata.subagent` as a route, not a dependency.
- [ ] Link specs from README.

Acceptance criteria:
- Specs are committed under `docs/specs/`.
- README links to the specs.
- Current implementation gaps are called out explicitly.
```

### Issue 2: Align skill artifact distribution with the Cloudflare RFC

Labels: `server`, `rfc-compatibility`

Body:

```markdown
Add archive artifact support for skills with resources while preserving existing per-file resource routes.

Tasks:
- [ ] Generate deterministic archives for resourceful skill versions.
- [ ] Add archive download endpoint.
- [ ] Emit `type: "archive"` in the RFC feed for resourceful skills.
- [ ] Keep `type: "skill-md"` for single-file skills.
- [ ] Preserve existing resource URLs for compatibility.

Acceptance criteria:
- RFC feed artifact digests verify the exact bytes at `url`.
- Archive entries contain `SKILL.md` at the archive root.
- Path traversal and absolute paths are rejected.
- Integration tests cover `skill-md` and `archive` entries.
```

### Issue 3: Implement `/manifest.json` native manifest root and skill collection

Labels: `server`, `manifest`

Body:

```markdown
Implement the initial native manifest tree for skills.

Tasks:
- [ ] Add `/manifest.json` root endpoint.
- [ ] Add `/manifest/skills/index.json`.
- [ ] Add path-based skills pages.
- [ ] Add skill identity index endpoints.
- [ ] Add skill version detail endpoints.
- [ ] Add source-generated JSON models.

Acceptance criteria:
- Clients can traverse from `/manifest.json` to a skill version detail by following `href` values.
- Skill version detail reuses the RFC artifact fields and values.
- Unsupported future collections can be ignored by clients.
```

### Issue 4: Add sub-agent storage and artifact endpoints

Labels: `server`, `subagents`

Body:

```markdown
Add first-class SkillServer support for versioned NetClaw sub-agent definitions.

Tasks:
- [ ] Add sub-agent tables and migrations.
- [ ] Add sub-agent domain/API models.
- [ ] Add parser/validator for NetClaw sub-agent markdown.
- [ ] Add upload endpoint.
- [ ] Add metadata endpoints.
- [ ] Add `agent.md` artifact download endpoint.
- [ ] Add delete/version management behavior consistent with skills.

Acceptance criteria:
- Valid sub-agent markdown uploads successfully.
- Invalid frontmatter returns actionable errors.
- Duplicate versions return conflict.
- Artifact digest verifies downloaded `agent.md` bytes.
```

### Issue 5: Add sub-agent collection to the native manifest

Labels: `server`, `manifest`, `subagents`

Body:

```markdown
Expose sub-agents through the native manifest without adding them to the RFC skill feed.

Tasks:
- [ ] Add `/manifest/subagents/index.json`.
- [ ] Add path-based sub-agent pages.
- [ ] Add sub-agent identity index endpoints.
- [ ] Add sub-agent version detail endpoints.
- [ ] Link sub-agent collection from `/manifest.json`.
- [ ] Link skill version `routesToSubagent` when `metadata.subagent` is present.

Acceptance criteria:
- Native clients can traverse from root to sub-agent version detail.
- Sub-agent details use `kind: "subagent-version"` and `type: "agent-md"`.
- RFC index output remains skill-only.
```

### Issue 6: Extend `Netclaw.SkillClient` for native manifest and sub-agents

Labels: `client`, `manifest`, `subagents`

Body:

```markdown
Add typed client support for native manifest traversal and sub-agent artifact operations.

Tasks:
- [ ] Add native manifest models.
- [ ] Add `GetManifestAsync`.
- [ ] Add skill manifest traversal helpers.
- [ ] Add sub-agent manifest traversal helpers.
- [ ] Add upload/download methods for sub-agent definitions.
- [ ] Add destination-agnostic helpers for verified artifact download.
- [ ] Document how non-NetClaw clients can provide destination and format adapters.
- [ ] Ensure helper APIs do not assume NetClaw application paths or formats.
- [ ] Preserve AOT source-generated serialization.

Acceptance criteria:
- Client can fetch root manifest and follow collection links.
- Client can download a sub-agent artifact and verify digest.
- Client APIs do not hard-code NetClaw application paths.
- Client APIs can be reused by an OpenCode or other non-NetClaw sync adapter.
- Existing RFC client methods remain unchanged.
```

### Issue 7: Extend CLI validation and publishing for sub-agents

Labels: `cli`, `subagents`

Body:

```markdown
Teach `skillserver` CLI how to validate and publish NetClaw sub-agent definitions.

Tasks:
- [ ] Add sub-agent validation command or extend lint command.
- [ ] Add publish command for a single sub-agent `.md` file.
- [ ] Add duplicate handling and force behavior.
- [ ] Add dry-run and verbose output.
- [ ] Update CLI README.

Acceptance criteria:
- CLI validates required and optional fields consistently with the server.
- CLI publishes valid sub-agent definitions.
- CLI reports actionable errors for invalid files.
```

### Issue 8: Add native manifest sync support in NetClaw

Labels: `netclaw-integration`, `subagents`, `manifest`

Body:

```markdown
Update NetClaw to sync SkillServer native manifest resources, including sub-agent definitions.

Tasks:
- [ ] Fetch `/manifest.json` for SkillServer feeds that support it.
- [ ] Fall back to RFC skill sync when native manifest is unavailable.
- [ ] Sync skill artifacts using native manifest skill details.
- [ ] Sync sub-agent `agent-md` artifacts into a managed server-feed location.
- [ ] Track sub-agent sync state by feed, name, version, and digest.
- [ ] Preserve user-authored local sub-agent files.
- [ ] Keep local sync destinations client-configurable or clearly scoped to the NetClaw home directory.
- [ ] Ensure skill `metadata.subagent` targets resolve after sync.

Acceptance criteria:
- NetClaw syncs sub-agents without overwriting user-authored definitions.
- Failed sync keeps existing local definitions.
- Removed remote definitions are pruned only after a confirmed successful sync.
```

### Issue 9: Harden security and compatibility for native sync

Labels: `security`, `testing`, `compatibility`

Body:

```markdown
Add end-to-end safety checks for native manifest sync and sub-agent artifacts.

Tasks:
- [ ] Threat-model archive downloads and sub-agent prompt distribution.
- [ ] Test digest mismatch rejection.
- [ ] Test path traversal rejection.
- [ ] Test auth behavior for manifest and artifact endpoints.
- [ ] Test unsupported resource kinds are ignored by clients.
- [ ] Document migration behavior for existing resourceful skills.

Acceptance criteria:
- Security-sensitive failure modes are covered by tests.
- Documentation states trust assumptions and sync safety behavior.
- Existing RFC feed consumers remain compatible.
```
