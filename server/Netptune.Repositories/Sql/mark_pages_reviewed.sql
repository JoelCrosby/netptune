UPDATE pages
SET reviewed_at = @ReviewedAt
  , reviewed_by_user_id = @UserId
WHERE id = ANY(@Ids)
  AND workspace_id = @WorkspaceId
  AND is_deleted = false
RETURNING id;
