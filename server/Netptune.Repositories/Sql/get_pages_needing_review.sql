WITH candidates AS (
    SELECT
          page.id
        , page.parent_id
        , page.title
        , page.slug
        , COALESCE(page.updated_at, page.created_at) AS updated_at
        , CASE
            WHEN EXISTS (
                SELECT 1
                FROM page_links link
                LEFT JOIN project_tasks task ON task.id = CASE WHEN link.target_type = 0 THEN link.target_id::int END
                LEFT JOIN pages target ON target.id = CASE WHEN link.target_type = 1 THEN link.target_id::int END
                WHERE link.page_id = page.id
                  AND (task.is_deleted OR target.is_deleted OR (link.target_type = 1 AND target.id IS NULL))
            ) THEN 2
            WHEN GREATEST(
                COALESCE(page.reviewed_at, page.created_at),
                COALESCE(page.updated_at, page.created_at)) < @StaleBefore THEN 0
            WHEN COALESCE(page.reviewed_at, page.created_at) < @UnvisitedBefore AND NOT EXISTS (
                SELECT 1
                FROM page_visits visit
                WHERE visit.page_id = page.id
                  AND visit.visited_at >= @UnvisitedBefore
            ) THEN 1
          END AS reason
    FROM pages page
    WHERE page.workspace_id = @WorkspaceId
      AND page.is_deleted = false
      AND page.is_template = false
      AND page.kind = 1
      AND page.status = 1
)
SELECT
      candidates.id
    , candidates.parent_id
    , candidates.title
    , candidates.slug
    , candidates.updated_at
    , candidates.reason
FROM candidates
WHERE candidates.reason IS NOT NULL
ORDER BY candidates.reason DESC, candidates.updated_at ASC
LIMIT @Take;
