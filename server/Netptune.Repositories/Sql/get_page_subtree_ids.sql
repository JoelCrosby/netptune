-- Returns @PageId and every page below it. Archived descendants are skipped unless
-- @IncludeArchived, so archiving a folder does not re-stamp pages archived earlier on their own.
-- Restore narrows the archived set further to the pages archived together with the root.
--
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
