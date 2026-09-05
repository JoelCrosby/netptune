-- Monthly assistant spend cap, in US dollars.
-- Null means no cap, which is what every existing workspace gets.

ALTER TABLE workspaces
    ADD COLUMN IF NOT EXISTS assistant_spend_cap numeric(12, 4) NULL;

ALTER TABLE workspaces
    DROP CONSTRAINT IF EXISTS ck_workspaces_assistant_spend_cap;

ALTER TABLE workspaces
    ADD CONSTRAINT ck_workspaces_assistant_spend_cap
    CHECK (assistant_spend_cap IS NULL OR assistant_spend_cap > 0);
