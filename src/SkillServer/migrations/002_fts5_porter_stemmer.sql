-- 002_fts5_porter_stemmer.sql: Enable Porter stemmer for better search matching

-- Drop existing triggers before dropping the table
DROP TRIGGER IF EXISTS trg_skills_fts_insert;
DROP TRIGGER IF EXISTS trg_skills_fts_delete;

-- Recreate FTS table with porter stemmer tokenizer
DROP TABLE IF EXISTS skills_fts;

CREATE VIRTUAL TABLE skills_fts USING fts5(
    name,
    description,
    category,
    tokenize='porter unicode61'
);

-- Recreate triggers to keep FTS in sync
CREATE TRIGGER trg_skills_fts_insert
AFTER INSERT ON skill_versions
WHEN NEW.is_latest = 1
BEGIN
    DELETE FROM skills_fts WHERE rowid = NEW.skill_id;
    INSERT INTO skills_fts(rowid, name, description, category)
    SELECT NEW.skill_id, s.name, NEW.description, COALESCE(NEW.category, '')
    FROM skills s WHERE s.id = NEW.skill_id;
END;

CREATE TRIGGER trg_skills_fts_delete
AFTER DELETE ON skill_versions
BEGIN
    DELETE FROM skills_fts WHERE rowid = OLD.skill_id;
    INSERT INTO skills_fts(rowid, name, description, category)
    SELECT s.id, s.name, sv.description, COALESCE(sv.category, '')
    FROM skills s
    JOIN skill_versions sv ON sv.skill_id = s.id AND sv.is_latest = 1
    WHERE s.id = OLD.skill_id;
END;

-- Repopulate FTS index from existing data
INSERT INTO skills_fts(rowid, name, description, category)
SELECT s.id, s.name, sv.description, COALESCE(sv.category, '')
FROM skills s
JOIN skill_versions sv ON sv.skill_id = s.id AND sv.is_latest = 1;
