-- Workspace-wide API keys for the AI assistant.
-- A member's own key still wins; this one covers everyone who has not added one.
-- New databases get this from EnsureCreated; an existing database needs this script.

CREATE TABLE IF NOT EXISTS workspace_ai_credentials (
    id uuid NOT NULL,
    workspace_id integer NOT NULL,
    provider integer NOT NULL,
    label character varying(128) NOT NULL,
    secret bytea NOT NULL,
    secret_hint character varying(8) NOT NULL,
    model character varying(128),
    created_by_user_id text,
    created_at timestamp with time zone NOT NULL,
    last_used_at timestamp with time zone,
    CONSTRAINT pk_workspace_ai_credentials PRIMARY KEY (id),
    CONSTRAINT fk_workspace_ai_credentials_workspaces_workspace_id FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_workspace_ai_credentials_workspace_id_provider
    ON workspace_ai_credentials (workspace_id, provider);
