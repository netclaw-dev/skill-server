-- 005_subagents.sql: First-class sub-agent registry support

CREATE TABLE IF NOT EXISTS subagents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE COLLATE NOCASE,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS subagent_versions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    subagent_id INTEGER NOT NULL REFERENCES subagents(id) ON DELETE CASCADE,
    version TEXT NOT NULL,
    description TEXT NOT NULL,
    model_role TEXT NOT NULL CHECK(model_role IN ('Compaction', 'Main')),
    timeout_seconds INTEGER NOT NULL,
    prefill_timeout_seconds INTEGER,
    visibility TEXT NOT NULL CHECK(visibility IN ('user-facing', 'internal')),
    emit_structured_findings INTEGER NOT NULL DEFAULT 0,
    sha256 TEXT NOT NULL,
    size_bytes INTEGER NOT NULL,
    published_at TEXT NOT NULL,
    is_latest INTEGER NOT NULL DEFAULT 0,
    UNIQUE(subagent_id, version)
);

CREATE INDEX IF NOT EXISTS idx_subagent_versions_latest
    ON subagent_versions(subagent_id) WHERE is_latest = 1;

CREATE INDEX IF NOT EXISTS idx_subagent_versions_sha256
    ON subagent_versions(sha256);
