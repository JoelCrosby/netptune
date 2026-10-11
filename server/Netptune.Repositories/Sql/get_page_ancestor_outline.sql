-- Returns the live pages above any of @PageIds (id, parent and title), so the search indexer can
-- build each page's folder path without loading the workspace's whole tree.
--
-- UNION (not UNION ALL) dedupes the frontier, so a corrupted parent loop still terminates.
WITH RECURSIVE ancestors AS (
    SELECT parent.id, parent.parent_id, parent.title
    FROM pages page
    INNER JOIN workspaces workspace ON workspace.id = page.workspace_id
    INNER JOIN pages parent ON parent.id = page.parent_id
    WHERE page.id = ANY(@PageIds)
      AND workspace.slug = @WorkspaceKey
      AND parent.workspace_id = page.workspace_id
      AND parent.is_deleted = false

    UNION

    SELECT parent.id, parent.parent_id, parent.title
    FROM pages parent
    INNER JOIN ancestors ON parent.id = ancestors.parent_id
    INNER JOIN pages child ON child.id = ancestors.id
    WHERE parent.workspace_id = child.workspace_id
      AND parent.is_deleted = false
)
SELECT
      ancestors.id
    , ancestors.parent_id
    , ancestors.title
FROM ancestors;
