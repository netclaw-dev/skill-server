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
