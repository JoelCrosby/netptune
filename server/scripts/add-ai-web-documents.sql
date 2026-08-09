-- Fetched web pages the assistant can page through without holding them in context.
-- Rows are disposable: expires_at drives cleanup, and a missing document just means a re-fetch.
-- New databases get this from EnsureCreated; an existing database needs this script.

CREATE TABLE IF NOT EXISTS ai_web_documents
(
    id uuid NOT NULL,
    workspace_id integer NOT NULL,
    conversation_id uuid NULL,
    requested_url character varying(2048) NOT NULL,
    final_url character varying(2048) NOT NULL,
    title character varying(512) NULL,
    content_type character varying(128) NULL,
    content text NOT NULL,
    character_count integer NOT NULL,
    fetched_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_ai_web_documents PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_ai_web_documents_workspace_id
    ON ai_web_documents (workspace_id);

CREATE INDEX IF NOT EXISTS ix_ai_web_documents_expires_at
    ON ai_web_documents (expires_at);
