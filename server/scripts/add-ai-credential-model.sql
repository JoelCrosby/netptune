-- Per-credential model override for the AI assistant.
-- Null means the server falls back to the configured default for that provider.

ALTER TABLE user_ai_credentials
    ADD COLUMN IF NOT EXISTS model character varying(128);
