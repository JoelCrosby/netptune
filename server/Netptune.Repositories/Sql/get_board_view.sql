-- Board view for BoardGroupRepository.GetBoardViewGroups. Returns two result sets:
-- one row per (group, task) with tags/assignees pre-aggregated as arrays, then the
-- board's workspace/project identifiers. @searchPhrase is null when no search is applied.
WITH board_groups_for_board AS (
    SELECT bg.id
         , bg.name
         , bg.status_id
         , bg.sort_order
    FROM board_groups bg
    WHERE bg.board_id = @boardId
      AND NOT bg.is_deleted
),
limited_tasks AS (
    SELECT pt.id               AS task_id
         , pt.name             AS task_name
         , pt.priority         AS task_priority
         , pt.estimate_type    AS task_estimate_type
         , pt.estimate_value   AS task_estimate_value
         , pt.start_date       AS task_start_date
         , pt.due_date         AS task_due_date
         , pt.created_at       AS task_created_at
         , pt.updated_at       AS task_updated_at
         , s.id                AS sprint_id
         , s.name              AS sprint_name
         , s.status            AS sprint_status
         , pt.project_scope_id AS project_scope_id
         , pt.status_id        AS task_status_id
         , st.name             AS task_status_name
         , st.key              AS task_status_key
         , st.color            AS task_status_color
         , st.category         AS task_status_category
         , ptibg.sort_order    AS task_sort_order
         , bg.id               AS board_group_id
         , pt.workspace_id     AS workspace_id
         , pt.project_id       AS project_id
    FROM board_groups_for_board bg
             INNER JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
             INNER JOIN project_tasks pt on pt.id = ptibg.project_task_id
                AND NOT pt.is_deleted
             INNER JOIN statuses st on pt.status_id = st.id
             LEFT JOIN sprints s on pt.sprint_id = s.id AND NOT s.is_deleted
    WHERE (@sprintId IS NULL OR pt.sprint_id = @sprintId)
      AND (@searchPhrase IS NULL
           OR to_tsvector('english', pt.name) @@ websearch_to_tsquery('english', @searchPhrase))
)
SELECT lt.task_id
     , lt.task_name
     , lt.task_priority
     , lt.task_estimate_type
     , lt.task_estimate_value
     , lt.task_start_date
     , lt.task_due_date
     , lt.task_created_at
     , lt.task_updated_at
     , lt.sprint_id
     , lt.sprint_name
     , lt.sprint_status
     , lt.project_scope_id
     , lt.task_status_id
     , lt.task_status_name
     , lt.task_status_key
     , lt.task_status_color
     , lt.task_status_category
     , lt.task_sort_order
     , bg.id               AS board_group_id
     , bg.name             AS board_group_name
     , bg.status_id        AS board_group_status_id
     , bg.sort_order       AS board_group_sort_order
     , lt.workspace_id
     , lt.project_id
     , EXISTS (
           SELECT 1
           FROM comments c
           WHERE c.entity_type = @taskEntityType
             AND c.entity_id = lt.task_id
             AND NOT c.is_deleted
       )                    AS has_comments
     , COALESCE((
           SELECT array_agg(t.name ORDER BY t.name)
           FROM project_task_tags ptt
                    INNER JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
           WHERE ptt.project_task_id = lt.task_id
       ), '{}')           AS tags
     , COALESCE((
           SELECT json_agg(json_build_object(
                       'id', u.id,
                       'firstname', u.firstname,
                       'lastname', u.lastname,
                       'picture_url', u.picture_url) ORDER BY u.id)
           FROM project_task_app_users ptau
                    INNER JOIN users u on ptau.user_id = u.id
           WHERE ptau.project_task_id = lt.task_id
       ), '[]')           AS assignees

FROM board_groups_for_board bg

         LEFT JOIN limited_tasks lt on bg.id = lt.board_group_id

ORDER BY bg.sort_order, bg.id, lt.task_sort_order, lt.task_id;

-- Select board

SELECT w.slug AS workspace_identifier
     , p.key  AS project_key
FROM boards b

         LEFT JOIN workspaces w on b.workspace_id = w.id
         LEFT JOIN projects p on b.project_id = p.id
WHERE b.id = @boardId;
