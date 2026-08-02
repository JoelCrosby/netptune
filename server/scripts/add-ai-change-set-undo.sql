-- Undo support for applied assistant change sets.
-- The undo payload is the entity state captured just before a change was applied.
-- New databases get these from EnsureCreated; an existing database needs this script.

ALTER TABLE ai_proposed_changes
    ADD COLUMN IF NOT EXISTS undo_payload jsonb;

ALTER TABLE ai_proposed_changes
    ADD COLUMN IF NOT EXISTS undone_at timestamp with time zone;

ALTER TABLE ai_change_sets
    ADD COLUMN IF NOT EXISTS undone_at timestamp with time zone;
