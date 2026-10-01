-- Boards in a workspace for BoardRepository.GetBoardViewModels, with per-board task stats and the
-- distinct users assigned to the board's tasks (busiest first) pre-aggregated as JSON.
SELECT b.id,
       b.name,
       b.identifier,
       b.project_id,
       b.board_type,
       CAST(b.created_at AS timestamp with time zone),
       CAST(b.updated_at AS timestamp with time zone),
       b.meta_info,
       (u.id IS NULL),
       u.firstname,
       u.lastname,
       p.name AS project_name,
       task_stats.task_count,
       task_stats.last_updated,
       board_assignees.assignees
FROM boards AS b
         INNER JOIN projects AS p ON b.project_id = p.id AND NOT p.is_deleted
         INNER JOIN workspaces AS w ON p.workspace_id = w.id AND NOT w.is_deleted
         LEFT JOIN users AS u ON b.owner_id = u.id
         LEFT JOIN LATERAL (
             SELECT COUNT(pt.id)      AS task_count,
                    MAX(pt.updated_at) AS last_updated
             FROM board_groups AS bg
                      INNER JOIN project_task_in_board_groups AS ptbg ON ptbg.board_group_id = bg.id
                      INNER JOIN project_tasks AS pt ON pt.id = ptbg.project_task_id AND NOT pt.is_deleted
             WHERE bg.board_id = b.id AND NOT bg.is_deleted
         ) AS task_stats ON TRUE
         LEFT JOIN LATERAL (
             SELECT COALESCE(json_agg(json_build_object(
                        'id', au.id,
                        'firstname', au.firstname,
                        'lastname', au.lastname,
                        'picture_url', au.picture_url,
                        'is_service_account', au.user_type = 1)
                        ORDER BY assigned.task_count DESC, au.firstname, au.lastname), '[]') AS assignees
             FROM (
                 SELECT ptau.user_id, COUNT(DISTINCT pt.id) AS task_count
                 FROM board_groups AS bg
                          INNER JOIN project_task_in_board_groups AS ptbg ON ptbg.board_group_id = bg.id
                          INNER JOIN project_tasks AS pt ON pt.id = ptbg.project_task_id AND NOT pt.is_deleted
                          INNER JOIN project_task_app_users AS ptau ON ptau.project_task_id = pt.id
                 WHERE bg.board_id = b.id AND NOT bg.is_deleted
                 GROUP BY ptau.user_id
             ) AS assigned
                      INNER JOIN users AS au ON au.id = assigned.user_id
         ) AS board_assignees ON TRUE
WHERE w.slug = @slug AND NOT b.is_deleted
ORDER BY COALESCE(p.updated_at, p.created_at) DESC, p.id DESC, COALESCE(b.updated_at, b.created_at) DESC, b.id DESC
OFFSET @skip
LIMIT @pageSize
