-- Adds assistant attribution to the event ledger.
-- origin_type: 0 = user, 1 = automation, 2 = system, 3 = assistant.
-- agent: display name of the assistant that produced the change, null for direct user action.

ALTER TABLE event_records
    ADD COLUMN IF NOT EXISTS origin_type integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS agent character varying(64);

-- ADD COLUMN IF NOT EXISTS is a no-op when the column already exists, so the default is set
-- again here. Without it, any writer that predates the column, such as a pod still serving
-- traffic mid rollout, inserts a row with no origin_type and fails the whole unit of work on
-- the not-null constraint.
ALTER TABLE event_records
    ALTER COLUMN origin_type SET DEFAULT 0;
