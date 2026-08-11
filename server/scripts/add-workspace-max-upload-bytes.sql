-- Per-workspace ceiling on the size of a single uploaded file.
-- Defaults to 50 MiB so existing workspaces keep the limit that used to be hard coded.

ALTER TABLE workspaces
    ADD COLUMN IF NOT EXISTS max_upload_bytes bigint NOT NULL DEFAULT 52428800;

ALTER TABLE workspaces
    DROP CONSTRAINT IF EXISTS ck_workspaces_max_upload_bytes;

ALTER TABLE workspaces
    ADD CONSTRAINT ck_workspaces_max_upload_bytes
        CHECK (max_upload_bytes >= 1048576 AND max_upload_bytes <= 536870912);
