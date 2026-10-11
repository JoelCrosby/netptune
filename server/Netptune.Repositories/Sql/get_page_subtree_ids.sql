-- UNION (not UNION ALL) dedupes the frontier, so a corrupted parent loop still terminates.
WITH RECURSIVE subtree AS (
    SELECT page.id
    FROM pages page
    WHERE page.id = @PageId
      AND page.workspace_id = @WorkspaceId

    UNION

    SELECT child.id
    FROM pages child
    INNER JOIN subtree ON child.parent_id = subtree.id
    WHERE child.workspace_id = @WorkspaceId
      AND (@IncludeArchived OR child.is_deleted = false)
)
SELECT subtree.id
FROM subtree;
