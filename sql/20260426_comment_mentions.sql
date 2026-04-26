-- Add comment_mentions table to support @mention in comments.
-- Stores one row per (comment, mentioned user) pair.
-- The ActivityHandler uses RecipientUserIds from the Mention activity event
-- to route notifications only to mentioned users rather than all workspace members.

CREATE TABLE comment_mentions (
    id                  serial          PRIMARY KEY,
    comment_id          integer         NOT NULL REFERENCES comments(id)  ON DELETE CASCADE,
    user_id             text            NOT NULL REFERENCES "users"(id) ON DELETE CASCADE,
    workspace_id        integer         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    owner_id            text            REFERENCES "users"(id)      ON DELETE SET NULL,
    created_by_user_id  text            REFERENCES "users"(id)      ON DELETE SET NULL,
    modified_by_user_id text            REFERENCES "users"(id)      ON DELETE SET NULL,
    deleted_by_user_id  text            REFERENCES "users"(id)      ON DELETE SET NULL,
    created_at          timestamp       NOT NULL DEFAULT NOW(),
    updated_at          timestamp,
    is_deleted          boolean         NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX ix_comment_mentions_comment_id_user_id
    ON comment_mentions (comment_id, user_id);

CREATE INDEX ix_comment_mentions_workspace_id
    ON comment_mentions (workspace_id);
