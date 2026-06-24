-- Export rows for every task in a workspace (capped at @take) for
-- TaskRepository.GetExportTasksAsync.
SELECT w.slug              AS workspace_key
     , p.name              AS project_name
     , p.key               AS project_key
     , b.name              AS board_name
     , b.identifier        AS board_identifier
     , pt.id               AS task_id
     , pt.name             AS task_name
     , pt.description      AS task_description
     , pt.project_scope_id AS project_scope_id
     , st.name             AS task_status
     , pt.created_at       AS task_created_at
     , pt.updated_at       AS task_updated_at
     , ptibg.sort_order    AS task_sort_order
     , bg.name             AS board_group_name
     , bg.type             AS board_group_type
     , bg.sort_order       AS board_group_sort_order
     , u.firstname         AS assignee_firstname
     , u.lastname          AS assignee_lastname
     , u.email             AS assignee_email
     , o.firstname         AS owner_firstname
     , o.lastname          AS owner_lastname
     , o.email             AS owner_email
     , t.name              AS tag
     , s.name              AS sprint_name
     , s.status            AS sprint_status
     , s.start_date        AS sprint_start_date
     , s.end_date          AS sprint_end_date

FROM workspaces w
         LEFT JOIN projects p on p.workspace_id = w.id
         LEFT JOIN boards b on b.project_id = p.id
         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id
            AND NOT pt.is_deleted
            AND pt.id IN (
                SELECT pt_limit.id
                FROM project_tasks pt_limit
                WHERE pt_limit.workspace_id = w.id
                  AND NOT pt_limit.is_deleted
                ORDER BY COALESCE(pt_limit.updated_at, pt_limit.created_at) DESC, pt_limit.id DESC
                LIMIT @take
            )
         INNER JOIN users o on pt.owner_id = o.id
         INNER JOIN statuses st on pt.status_id = st.id
         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
         LEFT JOIN project_task_app_users ptau on pt.id = ptau.project_task_id
         LEFT JOIN users u on ptau.user_id = u.id
         LEFT JOIN sprints s on pt.sprint_id = s.id AND NOT s.is_deleted

WHERE w.slug = @workspaceKey

ORDER BY p.id, b.identifier, bg.sort_order, ptibg.sort_order;
