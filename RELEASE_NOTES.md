#### 0.4.0 July 14th 2026 ####

**What's New**

This release adds a fully interactive gallery UI, first-class sub-agent support, and native manifest endpoints for destination-agnostic skill sync.

**New Features**
- Add interactive gallery UI with client-side search, skill version history navigation, and a resource browser for viewing SKILL.md and resource files
- Add first-class sub-agent support — storage endpoints, CLI commands (`publish-subagent`, `list-subagents`, `delete-subagent`, `download-subagent`, `lint subagent`), and batch publishing
- Add native manifest endpoints (`/manifest.json` and linked documents) for RFC-compatible skill and sub-agent sync
- Add deterministic `archive.zip` downloads for resourceful skills
- Add API versioning and native manifest client support in `Netclaw.SkillClient`
- Add copy button to code blocks in the gallery

**Improvements**
- Improve skill search relevance with exact and prefix name matching prioritized over description-only matches
- Preserve executable permission bits (Unix mode) for skill resources in downloaded archives

**Security**
- Resolve CVE-2026-49451 / GHSA-v5pm-xwqc-g5wc by pinning `Microsoft.OpenApi` to 2.7.5 (#104)
- Resolve GHSA-hv8m-jj95-wg3x (MessagePack/StreamJsonRpc LZ4 decompression DoS) by upgrading MessagePack to 3.1.8



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
