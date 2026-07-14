-- Filtered, sorted, paginated task list for TaskRepository.GetTasksAsync.
-- {taskOrder} and {rowOrder} are replaced at runtime with whitelisted ORDER BY
-- expressions (see GetTaskOrderBy / GetTaskRowOrderBy) before execution.
-- count(*) OVER() carries the unpaged total on every row.
WITH filtered_tasks AS (
    SELECT pt.id
         , pt.owner_id
         , pt.name
         , pt.description
         , pt.status_id
         , st.name AS status_name
         , st.key AS status_key
         , st.color AS status_color
         , st.category AS status_category
         , pt.project_scope_id
         , pt.priority
         , pt.estimate_type
         , pt.estimate_value
         , pt.due_date
         , pt.project_id
         , pt.sprint_id
         , pt.workspace_id
         , pt.created_at
         , pt.updated_at
         , w.slug AS workspace_key
         , p.key AS project_key
         , p.name AS project_name
         , s.name AS sprint_name
         , s.status AS sprint_status
         , (
               SELECT COUNT(*)
               FROM project_task_app_users ptau_sort
               WHERE ptau_sort.project_task_id = pt.id
           ) AS assignee_count
         , CASE
               WHEN NULLIF(CONCAT_WS(' ', o.firstname, o.lastname), '') IS NULL
                   THEN o.user_name
               ELSE CONCAT_WS(' ', o.firstname, o.lastname)
           END AS owner_username
         , o.picture_url AS owner_picture_url
         , CASE
               WHEN d.id IS NULL THEN NULL
               WHEN NULLIF(CONCAT_WS(' ', d.firstname, d.lastname), '') IS NULL
                   THEN d.user_name
               ELSE CONCAT_WS(' ', d.firstname, d.lastname)
           END AS deleted_by_username
         , d.picture_url AS deleted_by_picture_url
         , COUNT(*) OVER() AS total_count
    FROM project_tasks pt
             INNER JOIN workspaces w ON pt.workspace_id = w.id
             INNER JOIN statuses st ON pt.status_id = st.id
             LEFT JOIN projects p ON pt.project_id = p.id
             LEFT JOIN sprints s ON pt.sprint_id = s.id AND NOT s.is_deleted
             INNER JOIN users o ON pt.owner_id = o.id
             LEFT JOIN users d ON pt.deleted_by_user_id = d.id
    WHERE w.slug = @workspaceKey
      AND pt.is_deleted = @deleted
      AND (@projectId IS NULL OR pt.project_id = @projectId)
      AND (@sprintId IS NULL OR pt.sprint_id = @sprintId)
      AND (@excludeSprintId IS NULL OR pt.sprint_id IS NULL OR pt.sprint_id != @excludeSprintId)
      AND (@excludeTaskId IS NULL OR pt.id != @excludeTaskId)
      AND (@noSprint = FALSE OR pt.sprint_id IS NULL)
      AND (CARDINALITY(@statusIds) = 0 OR pt.status_id = ANY(@statusIds))
      AND (CARDINALITY(@statusCategories) = 0 OR st.category = ANY(@statusCategories))
      AND (CARDINALITY(@assignees) = 0 OR EXISTS (
          SELECT 1
          FROM project_task_app_users ptau_filter
          WHERE ptau_filter.project_task_id = pt.id
            AND ptau_filter.user_id = ANY(@assignees)
      ))
      AND (CARDINALITY(@tags) = 0 OR EXISTS (
          SELECT 1
          FROM project_task_tags ptt_filter
                   INNER JOIN tags t_filter ON ptt_filter.tag_id = t_filter.id AND NOT t_filter.is_deleted
          WHERE ptt_filter.project_task_id = pt.id
            AND t_filter.name = ANY(@tags)
      ))
      AND (@search = '' OR (
          LOWER(pt.name) LIKE @searchPattern
          OR LOWER(p.key) LIKE @searchPattern
          OR LOWER(p.name) LIKE @searchPattern
          OR EXISTS (
              SELECT 1
              FROM project_task_tags ptt_search
                       INNER JOIN tags t_search ON ptt_search.tag_id = t_search.id AND NOT t_search.is_deleted
              WHERE ptt_search.project_task_id = pt.id
                AND LOWER(t_search.name) LIKE @searchPattern
          )
      ))
    ORDER BY {taskOrder}
    LIMIT @pageSize
    OFFSET @skip
)
SELECT ft.total_count
     , ft.id AS task_id
     , ft.owner_id
     , ft.name AS task_name
     , ft.description AS task_description
     , ft.status_id AS task_status_id
     , ft.status_name AS task_status_name
     , ft.status_key AS task_status_key
     , ft.status_color AS task_status_color
     , ft.status_category AS task_status_category
     , ft.project_scope_id
     , ft.priority AS task_priority
     , ft.estimate_type AS task_estimate_type
     , ft.estimate_value AS task_estimate_value
     , ft.due_date AS task_due_date
     , ft.project_id
     , ft.sprint_id
     , ft.sprint_name
     , ft.sprint_status
     , ft.workspace_id
     , ft.workspace_key
     , ft.created_at AS task_created_at
     , ft.updated_at AS task_updated_at
     , ft.owner_username
     , ft.owner_picture_url
     , ft.deleted_by_username
     , ft.deleted_by_picture_url
     , ft.project_key
     , ft.project_name
     , t.name AS tag
     , u.id AS assignee_id
     , u.firstname AS assignee_firstname
     , u.lastname AS assignee_lastname
     , u.picture_url AS assignee_picture_url
FROM filtered_tasks ft
         LEFT JOIN project_task_tags ptt ON ft.id = ptt.project_task_id
         LEFT JOIN tags t ON ptt.tag_id = t.id AND NOT t.is_deleted
         LEFT JOIN project_task_app_users ptau ON ft.id = ptau.project_task_id
         LEFT JOIN users u ON ptau.user_id = u.id
ORDER BY {rowOrder}, t.name, u.id;
