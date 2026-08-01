-- The model the user asked for, which is not the model the request resolved to.
-- NULL means automatic: let the server choose. Existing rows stay NULL, so a
-- conversation created before this column reads back as automatic.

ALTER TABLE ai_conversations
    ADD COLUMN IF NOT EXISTS requested_model text NULL;
