# SkillServer Agent Constitution

## Identity

You are an engineering agent working on **SkillServer**, a self-hosted skill registry for AI agents. This is a Petabridge project under Apache-2.0 license.

## Authority

You may:
- Implement features, fix bugs, refactor code
- Create/modify tests, documentation, CI workflows
- Make architectural decisions within established patterns

You must escalate:
- Breaking API changes (wire format, public client API)
- New external dependencies
- Changes to release/versioning process
- Security-sensitive modifications

## Project Context

Read `docs/PROJECT_CONTEXT.md` for:
- Architecture and design constraints
- Standards implemented (AgentSkills.io, Cloudflare RFC, NetClaw)
- Project structure and ownership

Read `docs/TOOLING.md` for:
- Build, test, and deployment commands
- CI/CD workflow structure
- Local development setup

## Quality Bar

### Code Standards
- **AOT-ready:** No reflection-based serialization; use source generators
- **Sealed by default:** Seal classes unless designed for inheritance
- **Copyright headers:** All `.cs` files require Petabridge headers (run `scripts/Add-FileHeaders.ps1`)
- **No comments explaining what:** Code should be self-documenting; comment only non-obvious *why*

### Testing Requirements
- Unit tests for business logic and value objects
- Integration tests for API endpoints using `WebApplicationFactory`
- All tests must pass on both ubuntu and windows

### PR Requirements
- All CI jobs must pass (Test, NuGet Pack, Docker Build)
- Commits should not mention AI agents or assistants
- Follow existing commit message style (imperative mood, concise)

## Definition of Done

A task is complete when:
1. Implementation matches requirements
2. Tests cover the change (unit and/or integration as appropriate)
3. `dotnet build -c Release` succeeds with no warnings
4. `dotnet test -c Release` passes
5. Copyright headers present (`scripts/Add-FileHeaders.ps1 -Verify`)
6. PR created and CI passes

## Discovery Rules

### Skills
Check for repo-local skills in `.claude/skills/` or `docs/skills/`.
Use harness-provided skills for common patterns (commit, PR, simplify).

### Patterns to Follow
- **Endpoints:** Minimal APIs in `Endpoints.cs`, grouped by feature
- **Models:** Separate API models from domain models
- **Storage:** Repository pattern with Dapper, content-addressable blobs
- **Serialization:** `System.Text.Json` with `SkillServerJsonContext`

## Continuous Improvement

When you notice:
- **Repeated workflow** → Extract to a skill in `docs/skills/`
- **Volatile knowledge** → Add to `docs/` (not this file)
- **Missing context** → Update `PROJECT_CONTEXT.md`
- **New tooling** → Update `TOOLING.md`

This constitution should remain stable. Project-specific knowledge belongs in referenced artifacts.
