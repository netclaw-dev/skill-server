## 0.4.0-beta.4 (2026-07-07)

### New Features

- **Batch sub-agent publishing** — Added `publish-subagents` to validate and publish all sub-agent markdown files in a folder with optional `--version`, `--force`, and `--dry-run` options (`#121`)
- **Directory sub-agent linting** — Added `lint subagents` to validate every file and report duplicate sub-agent names before publishing

### Improvements

- **Skill search ranking** — Increased relevance for exact or prefix name matches over body and description matches
- **Gallery interaction upgrades** — Made skill cards clickable, added version-link navigation to version history rows, and surfaced an Enter-key hint in the search box

### Internal

- **Sub-agent parsing and testing** — Extended sub-agent metadata parsing for `version`/`metadata.version` and added unit coverage for batch publish and route helpers

---

## 0.4.0-beta.3 (2026-07-07)

### Features

- **Dynamic gallery UI with client-side rendering** — SPA-style folder routes via rewrite middleware, shared JS modules, and dynamic listing/detail views for skills and sub-agents fetching real data from API endpoints (#117)
- **API versioning and native manifest HATEOAS discovery** — All API endpoints versioned under `/api/v1/` prefix; native manifest includes version negotiation and search links for discovery (#116)
- **Tolerant frontmatter metadata parsing** — SKILL.md frontmatter now accepts rich YAML values (lists, nested mappings) without rejecting the skill, enabling natural `tags: [a, b, c]` syntax (#118)

### Security

- **Security hardening and trust model documentation** — Auth boundary tests for sub-agent write endpoints, path-traversal rejection tests, digest-mismatch verification, and new `docs/specs/security.md` documenting trust boundaries (#111)

### Improvements

- **Aspire dashboard with seed data support** — Dashboard enabled during development with optional `SeedDataService` that uploads sample skills/sub-agents on startup (controlled by `SkillServer:SeedData` config flag) (#118)

### Internal

- **E2E test infrastructure** — Playwright smoke tests using `Aspire.Hosting.Testing` that spin up the real AppHost with seed data to verify full-stack rendering (#117)
- **YamlDotNet updated from 17.1.0 to 18.1.0** (#113)

---

## 0.4.0-beta.2 (2026-06-12)

### Features

- **Reference file uploads for skills** — Skills can now include reference files that are served alongside the SKILL.md, enabling richer skill documentation with separate spec files, examples, and resources (#105)
- **Full-text search via FTS5** — Skills are now searchable using SQLite FTS5 indexing, supporting tokenized text search across title, description, and content (#104)
- **Update checking endpoint** — Clients can poll `/api/skills/updates` to detect when skills have been modified on the server since their last sync (#104)

### Improvements

- **Native manifest endpoint enhancements** — Added `search` and `updates` links to the `/.well-known/agent-skills/index.json` manifest for better HATEOAS discovery (#104)

### Internal

- **GitHub Container Registry (GHCR) publishing migration** — Docker images now publish to `ghcr.io/netclaw-dev/skill-server` instead of Docker Hub (#106)
- **Trusted publishing with OIDC** — NuGet packages publish using GitHub trusted publishing with OIDC token exchange, eliminating long-lived API keys (#107)
- **Dependency updates** — Bumped Aspire packages to 9.3.1 and other minor version bumps (#108, #109)

---

## 0.3.1 (2026-05-20)

### Bug Fixes

- **409 conflict on duplicate skill versions** — Fixed race condition where concurrent uploads of the same skill version could cause database conflicts (#98)

---

## 0.3.0 (2026-05-15)

### Features

- **Native manifest support** — New `/.well-known/agent-skills/index.json` endpoint that provides machine-readable discovery of available skills, following the Agent Skills Manifest specification (#85)
- **Sub-agent registry** — Skills can now declare sub-agents, which are stored and served alongside skill metadata (#90)

### Improvements

- **Skill metadata validation** — Added server-side validation for required skill metadata fields (title, version, description) (#88)

### Internal

- **Database migration to SQLite** — Migrated from in-memory storage to SQLite for persistent skill storage across restarts (#82)
- **CI/CD pipeline updates** — Added build and test workflows for pull request validation (#80)

---

## 0.2.1 (2026-04-28)

### Bug Fixes

- **Publishing setup fix** — Corrected package metadata and build configuration for proper NuGet publishing (#75)

---

## 0.2.0 (2026-04-15)

### Features

- **Skill CRUD API** — Full REST API for creating, reading, updating, and deleting skills (#60)
- **Basic web UI** — Simple admin interface for managing skills (#62)

### Internal

- **Initial project scaffolding** — .NET Aspire application with SkillServer API and AppHost (#55)
