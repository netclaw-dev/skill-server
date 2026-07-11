#### 0.4.0-beta.6 July 10th 2026 ####

**New Features**
- Add `list-subagents` and `delete-subagent` CLI commands to enumerate and remove published sub-agent versions, bringing the `skillserver` CLI to parity with the existing skill `list`/`delete` commands (#129)

#### 0.4.0-beta.5 July 8th 2026 ####

**New Features**
- Add gallery resource browser — expose skill resource metadata and render a read-only file browser in the gallery for SKILL.md and resource files
- Add runtime gallery version display — expose SkillServer assembly metadata through `/api/v1/info` and render gallery footer versions from runtime app metadata

**Bug Fixes**
- Fix `?q=` query param not being read on skills listing page — navigating from homepage search to `/skills/?q=foo` now pre-fills the search box, shows filtered results, and updates the heading to 'Search Results' (#124)

**Internal**
- Remove duplicate `release_notes.md` (lowercase variant) to prevent checkout conflicts on case-insensitive filesystems (Windows)

#### 0.4.0-beta.4 July 7th 2026 ####

**New Features**
- Add batch sub-agent publishing with a new `publish-subagents` CLI command to validate and upload all sub-agent definitions under a folder, with support for version overrides, dry-run, force publish, and verbose output (#121)
- Add `lint subagents` CLI mode to validate every sub-agent markdown file in a directory and report duplicate sub-agent names

**Improvements**
- Improve skill search relevance by prioritizing exact and prefix skill-name matches ahead of description-only matches (#121)
- Improve gallery UX by making cards interactive, wiring version history rows to version URLs, and adding a keyboard hint to the search input

**Internal**
- Extend sub-agent validation and publishing flow to accept `version` frontmatter (including `metadata.version`) and add validation tests for batch publish and route parsing logic (#121)

#### 0.4.0-beta.2 July 3rd 2026 ####

**New Features**
- Add native manifest endpoints (`/manifest.json` and linked skill identity/version documents) for destination-agnostic, RFC-compatible sync (#96)
- Add first-class sub-agent support — `/subagents` storage with upload, list, delete, and `agent.md` download endpoints, and NetClaw sub-agent markdown frontmatter validation (#97)
- Add native sub-agent manifest endpoints (`/manifest/subagents/...`) that traverse from the root native manifest alongside skills (#98)
- Add sub-agent support to `Netclaw.SkillClient` — upload, list, download, delete, and native manifest traversal helpers (#99)
- Add CLI sub-agent commands: `lint subagent`, `publish-subagent`, and `download-subagent` for validating, publishing, and syncing sub-agents from the command line (#100)
- Add deterministic `archive.zip` downloads for resourceful skills, backfilled for existing skill versions (#95)

**Improvements**
- Preserve executable permission bits (Unix mode) for skill resources — packaged helper scripts in downloaded archives no longer need a manual `chmod` (#103)

**CI/CD**
- Skip the Docker `latest` tag and mark GitHub releases as prerelease automatically when publishing a prerelease tag (#101)

**Security**
- Resolve CVE-2026-49451 / GHSA-v5pm-xwqc-g5wc by pinning the transitive `Microsoft.OpenApi` dependency to 2.7.5 (#104)
- Resolve GHSA-hv8m-jj95-wg3x (MessagePack/StreamJsonRpc LZ4 decompression DoS) by upgrading MessagePack from 2.5.301 to 3.1.7 (#76, #80)
- Suppress GHSA-2m69-gcr7-jv3q (SQLitePCLRaw) pending an upstream patched release (#76)

**Dependency Updates**
- Bump Microsoft.Data.Sqlite from 10.0.7 to 10.0.9 (#106)
- Bump Microsoft.Extensions.Http from 10.0.7 to 10.0.9 (#107)
- Bump Dapper from 2.1.72 to 2.1.79 (#78)
- Bump Microsoft.AspNetCore.OpenApi from 10.0.7 to 10.0.9 (#83)

#### 0.3.1 May 15th 2026 ####

**New Features**
- Add `lint` command to CLI — validates local skills against the AgentSkills.io spec without requiring a server connection (#68)

**Bug Fixes**
- Fix unbound variable error in `install-skillserver.sh` — resolves installation failures on strict Bash environments (#60)

#### 0.3.0 May 5th 2026 ####

**New Features**
- Add `skillserver` CLI tool for publishing and managing skills from the command line (#56)
  - Commands: `publish`, `publish-all`, `delete`, `list`, `versions`, `verify`, `config`, `api-key`
  - Distributed as a .NET global tool (`dotnet tool install -g Netclaw.SkillServer.Cli`) and standalone trimmed binaries for linux-x64, linux-arm64, osx-arm64, and win-x64
  - Install scripts for Linux/macOS (`install-skillserver.sh`) and Windows (`install-skillserver.ps1`)
- Add `UploadSkillWithResourcesAsync` and `UploadSkillIfNotExistsAsync` to `Netclaw.SkillClient` for idempotent publishing with resource file support (#56)

**Improvements**
- Consolidate `publish_nuget.yml` and `publish_container.yml` into a unified `release.yml` workflow — NuGet packages, CLI binaries, and container images build in parallel with a single coordinated publish stage (#56)
- Add CLI publish dry-run, trim warning checks, and install script linting to PR validation (#56)

**Dependency Updates**
- Bump YamlDotNet from 17.0.1 to 17.1.0 (#55)

#### 0.2.1 April 29th 2026 ####

**New Features**
- Add resource file upload support to `POST /skills` — skills can now include arbitrary resource files (e.g. `references/guide.md`, `scripts/setup.sh`) per the AgentSkills.io spec (#52)

**Improvements**
- Resource upload endpoint now supports any subdirectory per the AgentSkills.io spec — previously restricted to `references/` only; all subdirectories are now accessible with path traversal protection (#53)
- Fix stream leak on validation failure in resource upload path (#53)

#### 0.2.0 April 27th 2026 ####

**Improvements**
- Add lightweight database migration system — schema changes are now applied automatically via numbered SQL migration files on server startup (#44)
- Enable FTS5 Porter stemmer for skill search — stemmed queries like "closing" now match skills containing "close deal", improving search relevance (#43)

#### 0.1.2 April 27th 2026 ####

**Improvements**
- Return 409 Conflict for duplicate skill version uploads instead of 400 Bad Request (#41)
  - Follows NuGet pattern — enables idempotent publish pipelines via `--skip-duplicate` semantics

#### 0.1.1 April 24th 2026 ####

**Bug Fixes**
- Fix /health endpoint NotSupportedException from source-generated JSON serializer (#39)

**CI/CD**
- Add --skip-duplicate to NuGet push commands to prevent duplicate package errors (#36)

**Dependency Updates**
- Bump YamlDotNet from 16.3.0 to 17.0.1 (#38)

#### 0.1.0 April 23rd 2026 ####

Initial release of SkillServer and Netclaw.SkillClient.

**SkillServer**
- Self-hosted skill registry for AI agent skills
- AgentSkills.io SKILL.md standard support
- Cloudflare Agent Skills Discovery RFC v0.2.0 compliance
- FTS5 full-text search across skill content
- Content-addressable blob storage (SHA-256)
- API key authentication for write operations with SHA-256 hashing
- SQLite-backed metadata with Dapper
- SDK container support with linux-x64 and linux-arm64 images
- Batch update checking endpoint

**Netclaw.SkillClient**
- Typed .NET client library for SkillServer
- AOT-compatible with source generators
- Full-text search, version resolution, and batch update checking
