-- 003_skill_artifact_metadata.sql: Track canonical artifact bytes separately from SKILL.md bytes

ALTER TABLE skill_versions ADD COLUMN artifact_sha256 TEXT;
ALTER TABLE skill_versions ADD COLUMN artifact_size_bytes INTEGER;

UPDATE skill_versions
SET artifact_sha256 = sha256
WHERE artifact_sha256 IS NULL;

UPDATE skill_versions
SET artifact_size_bytes = size_bytes
WHERE artifact_size_bytes IS NULL;

CREATE INDEX IF NOT EXISTS idx_skill_versions_artifact_sha256
    ON skill_versions(artifact_sha256);
