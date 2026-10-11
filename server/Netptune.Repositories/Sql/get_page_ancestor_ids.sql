WITH RECURSIVE ancestors AS (
    SELECT page.id, page.parent_id
    FROM pages page
    WHERE page.id = @PageId
      AND page.workspace_id = @WorkspaceId

    UNION

    SELECT parent.id, parent.parent_id
    FROM pages parent
    INNER JOIN ancestors ON parent.id = ancestors.parent_id
    WHERE parent.workspace_id = @WorkspaceId
)
SELECT ancestors.id
FROM ancestors;
