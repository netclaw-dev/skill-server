-- 001_initial_schema.sql: Initial schema (retrofitted from DatabaseInitializer.cs)

CREATE TABLE IF NOT EXISTS skills (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE COLLATE NOCASE,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS skill_versions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    version TEXT NOT NULL,
    description TEXT NOT NULL,
    category TEXT,
    skill_type TEXT NOT NULL CHECK(skill_type IN ('skill-md', 'archive')),
    sha256 TEXT NOT NULL,
    size_bytes INTEGER NOT NULL,
    published_at TEXT NOT NULL,
    is_latest INTEGER NOT NULL DEFAULT 0,
    UNIQUE(skill_id, version)
);

CREATE TABLE IF NOT EXISTS skill_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    skill_version_id INTEGER NOT NULL REFERENCES skill_versions(id) ON DELETE CASCADE,
    relative_path TEXT NOT NULL,
    sha256 TEXT NOT NULL,
    size_bytes INTEGER NOT NULL,
    UNIQUE(skill_version_id, relative_path)
);

CREATE INDEX IF NOT EXISTS idx_skill_versions_latest
    ON skill_versions(skill_id) WHERE is_latest = 1;

CREATE INDEX IF NOT EXISTS idx_skill_versions_sha256
    ON skill_versions(sha256);

CREATE INDEX IF NOT EXISTS idx_skill_files_sha256
    ON skill_files(sha256);

CREATE TABLE IF NOT EXISTS api_keys (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    label TEXT NOT NULL,
    key_hash TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL,
    expires_at TEXT
);

CREATE INDEX IF NOT EXISTS idx_api_keys_hash
    ON api_keys(key_hash);

-- Full-text search index for skill discovery
CREATE VIRTUAL TABLE IF NOT EXISTS skills_fts USING fts5(
    name,
    description,
    category
);

-- Keep FTS in sync when new versions are published
CREATE TRIGGER IF NOT EXISTS trg_skills_fts_insert
AFTER INSERT ON skill_versions
WHEN NEW.is_latest = 1
BEGIN
    DELETE FROM skills_fts WHERE rowid = NEW.skill_id;
    INSERT INTO skills_fts(rowid, name, description, category)
    SELECT NEW.skill_id, s.name, NEW.description, COALESCE(NEW.category, '')
    FROM skills s WHERE s.id = NEW.skill_id;
END;

-- Keep FTS in sync when versions are deleted
CREATE TRIGGER IF NOT EXISTS trg_skills_fts_delete
AFTER DELETE ON skill_versions
BEGIN
    DELETE FROM skills_fts WHERE rowid = OLD.skill_id;
    INSERT INTO skills_fts(rowid, name, description, category)
    SELECT s.id, s.name, sv.description, COALESCE(sv.category, '')
    FROM skills s
    JOIN skill_versions sv ON sv.skill_id = s.id AND sv.is_latest = 1
    WHERE s.id = OLD.skill_id;
END;

-- Backfill FTS from existing data (idempotent for existing databases)
INSERT INTO skills_fts(rowid, name, description, category)
SELECT s.id, s.name, sv.description, COALESCE(sv.category, '')
FROM skills s
JOIN skill_versions sv ON sv.skill_id = s.id AND sv.is_latest = 1
WHERE s.id NOT IN (SELECT rowid FROM skills_fts);
