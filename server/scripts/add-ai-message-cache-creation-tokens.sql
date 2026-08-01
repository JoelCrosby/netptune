-- Cache writes were previously indistinguishable from cache misses, because only
-- the read count was recorded. Existing rows keep zero, which is what they were.

ALTER TABLE ai_messages
    ADD COLUMN IF NOT EXISTS cache_creation_tokens integer NOT NULL DEFAULT 0;
