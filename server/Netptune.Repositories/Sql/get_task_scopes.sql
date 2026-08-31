-- Every scope the given tasks currently sit in, used by AncestorRepository.GetTaskScopes to decide
-- which per-scope notification subscriptions a task event matches. A task can live on several boards
-- at once, so this returns one row per board group membership rather than the single ancestor chain
-- get_project_task_ancestors resolves. Soft-deleted scopes drop out: a subscription to a deleted
-- board, sprint or project must stop delivering, and a board group is only reachable through a
-- board that is still live.
SELECT
      pt.id AS task_id
    , p.id  AS project_id
    , s.id  AS sprint_id
    , b.id  AS board_id
    , CASE WHEN b.id IS NULL THEN NULL ELSE bg.id END AS board_group_id
FROM project_tasks pt
LEFT JOIN projects p ON p.id = pt.project_id AND NOT p.is_deleted
LEFT JOIN sprints s ON s.id = pt.sprint_id AND NOT s.is_deleted
LEFT JOIN project_task_in_board_groups ptibg ON ptibg.project_task_id = pt.id
LEFT JOIN board_groups bg ON bg.id = ptibg.board_group_id AND NOT bg.is_deleted
LEFT JOIN boards b ON b.id = bg.board_id AND NOT b.is_deleted
WHERE pt.id = ANY(@taskIds);
