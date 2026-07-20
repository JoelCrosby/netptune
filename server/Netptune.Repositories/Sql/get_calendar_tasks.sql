SELECT COUNT(*) OVER() AS total_count
     , pt.id AS task_id
     , pt.project_scope_id
     , pt.name AS task_name
     , p.id AS project_id
     , p.name AS project_name
     , p.key AS project_key
     , st.id AS status_id
     , st.name AS status_name
     , st.key AS status_key
     , st.color AS status_color
     , st.category AS status_category
     , pt.priority
     , pt.start_date
     , pt.due_date
     , pt.sprint_id
     , COALESCE((
           SELECT json_agg(json_build_object(
                       'id', u.id,
                       'displayName', COALESCE(NULLIF(TRIM(CONCAT_WS(' ', u.firstname, u.lastname)), ''), u.user_name),
                       'pictureUrl', u.picture_url,
                       'isServiceAccount', u.user_type = 1)
                   ORDER BY u.id)
           FROM project_task_app_users ptau
                    INNER JOIN users u ON ptau.user_id = u.id
           WHERE ptau.project_task_id = pt.id
       ), '[]')::text AS assignees
FROM project_tasks pt
         INNER JOIN projects p ON pt.project_id = p.id AND NOT p.is_deleted
         INNER JOIN statuses st ON pt.status_id = st.id AND NOT st.is_deleted
WHERE pt.workspace_id = @workspaceId
  AND NOT pt.is_deleted
  AND pt.project_id = ANY(@allowedProjectIds)
  AND (@projectId IS NULL OR pt.project_id = @projectId)
  AND (@sprintId IS NULL OR pt.sprint_id = @sprintId)
  AND COALESCE(pt.start_date, pt.due_date) <= @date
  AND COALESCE(pt.due_date, pt.start_date) >= @date
ORDER BY {taskOrder}
LIMIT @pageSize OFFSET @skip;
