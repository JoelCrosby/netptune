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
                       'pictureUrl', u.picture_url)
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
  AND (CARDINALITY(@projectIds) = 0 OR pt.project_id = ANY(@projectIds))
  AND (CARDINALITY(@sprintIds) = 0 OR pt.sprint_id = ANY(@sprintIds))
  AND (
      (@unscheduled AND pt.start_date IS NULL AND pt.due_date IS NULL)
      OR
      (NOT @unscheduled AND (
          (pt.start_date IS NOT NULL AND pt.due_date IS NOT NULL
              AND pt.start_date <= @to AND pt.due_date >= @from)
          OR (pt.start_date IS NULL AND pt.due_date BETWEEN @from AND @to)
          OR (pt.due_date IS NULL AND pt.start_date BETWEEN @from AND @to)
      ))
  )
ORDER BY COALESCE(pt.start_date, pt.due_date), p.name, pt.project_scope_id, pt.id
LIMIT @take;
