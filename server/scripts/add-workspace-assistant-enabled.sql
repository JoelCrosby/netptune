-- Workspace-level switch for the AI assistant.
-- Defaults to true so existing workspaces keep the behaviour they already had.

ALTER TABLE workspaces
    ADD COLUMN IF NOT EXISTS assistant_enabled boolean NOT NULL DEFAULT TRUE;
