INSERT INTO reactions (
    comment_id,
    workspace_id,
    owner_id,
    created_by_user_id,
    value,
    created_at,
    updated_at,
    is_deleted
)
VALUES (
    @comment_id,
    @workspace_id,
    @owner_id,
    @owner_id,
    @value,
    NOW(),
    NOW(),
    FALSE
)
ON CONFLICT (comment_id, owner_id, value) DO NOTHING;
