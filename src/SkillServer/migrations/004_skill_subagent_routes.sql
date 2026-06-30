-- 004_skill_subagent_routes.sql: Persist optional skill-to-sub-agent route metadata

ALTER TABLE skill_versions ADD COLUMN routes_to_subagent TEXT;

CREATE INDEX IF NOT EXISTS idx_skill_versions_routes_to_subagent
    ON skill_versions(routes_to_subagent)
    WHERE routes_to_subagent IS NOT NULL;
