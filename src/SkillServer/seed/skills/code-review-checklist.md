---
name: code-review-checklist
description: Systematic code review checklist for .NET services covering correctness, tests, performance, security, and maintainability
license: Apache-2.0
metadata:
  category: code-quality
  version: "1.0"
  tags: [.net, code-review, testing, best-practices]
---

# Code Review Checklist

Apply this checklist to pull requests for .NET services. The goal is to catch defects before merge, enforce project standards, and leave the codebase easier to maintain than you found it.

## Scope and Context

Before reading changed lines, verify the PR matches its description:

- The title summarizes the change in imperative mood
- The description explains why the change exists, not only what changed
- Related issues or tickets are linked
- Migration steps, config changes, or dependency bumps call out any required action for operators

If the PR is larger than 400 lines of changed code outside tests, recommend splitting it. Large PRs reduce review quality and increase merge risk.

## Correctness

Check that the implementation matches requirements without introducing regressions:

- Public API surface changes are intentional and documented
- New enums, exception types, or error codes include values for expected failure modes
- Null handling is explicit where null is valid input; use nullable reference types and annotations to make intent visible
- String formatting uses interpolation or named arguments instead of positional placeholders that break when reordered
- Time-sensitive logic uses clocks or time providers rather than `DateTime.Now` so tests can control time
- File, stream, and connection disposal is guaranteed via `using` statements or scoped lifetime management

For .NET-specific APIs, prefer strongly typed options over magic strings. Use `Span<T>`/`ReadOnlySpan<T>` for performance-sensitive parsing, but do not force spans through public APIs unless the caller benefits from allocation avoidance.

## Tests

Verify that tests provide confidence proportional to the risk of the change:

- Unit tests cover business rules, value objects, and pure functions
- Integration tests validate API endpoints with `WebApplicationFactory` when the change touches HTTP behavior
- Tests assert behavior, not implementation. Avoid asserting private state or internal method calls unless the public outcome is impossible to observe
- Test data covers boundary values: empty collections, zero, negative, whitespace-only strings, and maximum lengths for fixed-size fields
- Async tests use `await` instead of `.Result` or `.Wait()` so exceptions propagate correctly
- Tests run deterministically. Flaky tests caused by time, random order, or shared state must be fixed in the same PR

If a change modifies serialization behavior, include tests that verify both write and read roundtrips. For JSON, assert the serialized shape matches expected output using `JsonSerializer.Serialize` with the project's source generator context.

## Performance

Flag changes that affect throughput, latency, or memory usage:

- Loops over collections use appropriate data structures; avoid O(n^2) nested iteration where one collection is large
- LINQ queries materialize results once when the result is used multiple times; calling `.ToList()` twice on an `IQueryable` executes the query twice
- String concatenation in loops uses `StringBuilder` or string interpolation outside the loop
- New allocations on hot paths are justified with benchmarks or profiling data
- Caching adds expiration, invalidation, and size limits to prevent unbounded memory growth

For database access:

- Queries select only required columns; avoid `SELECT *`
- Filters use indexed columns so queries scale with data volume
- Bulk operations use batching or bulk insert APIs instead of row-by-row round trips

If the PR includes benchmark code, verify benchmarks measure the changed path and report relevant metrics (mean, p95, allocations).

## Security

Check for common vulnerabilities without assuming external threat models:

- User input is validated before use; validation errors return structured error responses
- Sensitive data is not logged, traced, or included in exception messages
- Authorization checks happen at the handler level, not only inside business methods that might be reused elsewhere
- File paths are sanitized to prevent path traversal when writing or reading user-supplied names
- Redirect URLs are validated against allowed hosts before use in redirects
- Cryptographic operations use approved algorithms and key management; do not roll custom encryption

For .NET apps:

- `AntiForgeryToken` validation is enabled on state-changing endpoints
- Model binding does not overwrite non-bound properties by default
- Exception handling preserves internal details for diagnostics while returning safe messages to callers

If the PR touches authentication, authorization, or secrets handling, escalate review to a security-focused reviewer.

## Maintainability

Ensure the code remains readable and modifiable:

- Names describe intent; avoid abbreviations that require decoding
- Methods do one thing; if a method is longer than 40 lines, check whether it contains multiple responsibilities
- Comments explain non-obvious constraints or tradeoffs; remove comments that repeat what the code says
- Error messages include enough context to diagnose failures without requiring source access
- New files follow existing folder structure and naming conventions
- XML documentation is present on public APIs and updated when signatures change

For configuration:

- Options use `IOptions<T>` with validation at startup
- Required configuration keys fail fast during host build rather than returning null later
- Default values are documented in the same place as the option definition

## Build and Hygiene

Run local checks before approving:

- `dotnet build -c Release` succeeds with no warnings
- `dotnet test -c Release` passes on ubuntu and windows
- Slopwatch analysis reports no new violations
- Copyright headers are present on new files
- `git diff --check` shows no trailing whitespace or tab issues

If the project uses analyzers, treat analyzer warnings as build errors. Disable an analyzer only with a code comment that explains why the default rule is wrong for this case.

## Decision Guidance

Use these guidelines to decide how to respond:

- Request changes when correctness, security, or tests are insufficient
- Suggest improvements for performance and maintainability without blocking merge unless they affect production risk
- Approve with comments when the change is low-risk and suggestions are optional
- Ask for clarification when requirements conflict with implementation but you cannot determine intent from the PR description

Leave review comments at the line that needs attention. Summarize high-level concerns in the PR review comment so the author sees the full picture before addressing individual lines.

## Common Patterns to Accept

These patterns are acceptable and do not require change:

- Using `ArgumentNullException` for invalid required parameters
- Returning empty collections instead of null from methods that return collections
- Adding new properties to existing DTOs when backward compatibility is maintained
- Updating dependency versions with matching test coverage
- Refactoring private methods without changing public behavior or tests

## Common Patterns to Reject

These patterns require correction before merge:

- Catching `Exception` and swallowing it without logging
- Accessing shared mutable state from async code without synchronization
- Storing secrets in configuration files checked into source control
- Changing exception types thrown by public methods without updating callers
- Adding new dependencies without updating tests, documentation, or compatibility checks