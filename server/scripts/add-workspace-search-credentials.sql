-- Per-workspace web search credential for the assistant's web_search tool.
-- The secret is encrypted with ASP.NET Data Protection, so it is bytea and never readable from SQL.
-- SearXNG has no key: it stores an endpoint instead, and secret stays null.
-- New databases get this from EnsureCreated; an existing database needs this script.

CREATE TABLE IF NOT EXISTS workspace_search_credentials
(
    id uuid NOT NULL,
    workspace_id integer NOT NULL,
    provider integer NOT NULL,
    secret bytea NULL,
    secret_hint character varying(8) NOT NULL,
    engine_id character varying(128) NULL,
    endpoint character varying(2048) NULL,
    created_by_user_id text NULL,
    created_at timestamp with time zone NOT NULL,
    last_used_at timestamp with time zone NULL,
    CONSTRAINT pk_workspace_search_credentials PRIMARY KEY (id),
    CONSTRAINT fk_workspace_search_credentials_workspaces_workspace_id
        FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_workspace_search_credentials_workspace_id
    ON workspace_search_credentials (workspace_id);
