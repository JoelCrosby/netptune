-- Adds assistant attribution to the activity feed and puts the agent in the merge key.
--
-- The merge key gains agent so an assistant-applied change never merges into the same open entry as
-- the user's own edit. Agent is NOT NULL with an empty default on purpose: Postgres treats NULLs as
-- distinct in a unique index, so a nullable column here would stop ordinary user entries from ever
-- conflicting, and therefore from ever merging.

ALTER TABLE activity_entries
    ADD COLUMN IF NOT EXISTS agent character varying(64) NOT NULL DEFAULT '';

DROP INDEX IF EXISTS ux_activity_entries_open;

CREATE UNIQUE INDEX ux_activity_entries_open
    ON activity_entries (workspace_id, entity_type, entity_id, user_id, agent)
    WHERE is_open AND NOT is_deleted;
