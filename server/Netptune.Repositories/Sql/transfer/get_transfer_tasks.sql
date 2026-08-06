-- One row per task for TransferRepository.GetTaskPage, keyset-paginated by id so a
-- whole-workspace export never materialises. Assignees and tags come back as arrays
-- rather than as a task x assignee x tag fan-out. count(*) OVER() carries the number
-- of rows remaining from @afterId, so a first page with @afterId = 0 also yields the
-- unpaged total.
--
-- A task can sit in groups on more than one board, so @boardIdentifiers is matched with
-- EXISTS over every placement. Narrowing to one placement first and filtering on that
-- dropped any task whose lowest-id placement happened to be on a different board.
SELECT pt.id                        AS id
     , COUNT(*) OVER()              AS total_count
     , p.key                        AS project_key
     , pt.project_scope_id          AS project_scope_id
     , pt.name                      AS name
     , pt.description               AS description
     , st.key                       AS status_key
     , pt.priority                  AS priority
     , pt.estimate_type             AS estimate_type
     , pt.estimate_value            AS estimate_value
     , pt.start_date                AS start_date
     , pt.due_date                  AS due_date
     , s.name                       AS sprint_name
     , sp.key                       AS sprint_project_key
     , placement.board_identifier   AS board_identifier
     , placement.board_group_name   AS board_group_name
     , c.email                      AS created_by_email
     , pt.created_at                AS created_at
     , pt.updated_at                AS updated_at
     , ARRAY(
           SELECT u.email
           FROM project_task_app_users ptau
                    INNER JOIN users u ON ptau.user_id = u.id
           WHERE ptau.project_task_id = pt.id
           ORDER BY u.email
       )                            AS assignee_emails
     , ARRAY(
           SELECT t.name
           FROM project_task_tags ptt
                    INNER JOIN tags t ON ptt.tag_id = t.id AND NOT t.is_deleted
           WHERE ptt.project_task_id = pt.id
           ORDER BY t.name
       )                            AS tag_names

FROM project_tasks pt
         INNER JOIN statuses st ON pt.status_id = st.id
         LEFT JOIN projects p ON pt.project_id = p.id
         LEFT JOIN sprints s ON pt.sprint_id = s.id AND NOT s.is_deleted
         LEFT JOIN projects sp ON s.project_id = sp.id
         LEFT JOIN users c ON pt.created_by_user_id = c.id
         -- The one placement worth reporting. When the export is scoped to boards, the group on a
         -- requested board wins over an older placement elsewhere; unscoped, this is the earliest
         -- placement as before. Deleted groups are passed over rather than reported as no group.
         LEFT JOIN LATERAL (
             SELECT bg_pick.name       AS board_group_name
                  , b_pick.identifier  AS board_identifier
             FROM project_task_in_board_groups ptibg
                      INNER JOIN board_groups bg_pick ON ptibg.board_group_id = bg_pick.id AND NOT bg_pick.is_deleted
                      INNER JOIN boards b_pick ON bg_pick.board_id = b_pick.id
             WHERE ptibg.project_task_id = pt.id
             ORDER BY (CARDINALITY(@boardIdentifiers::text[]) = 0
                           OR LOWER(b_pick.identifier) = ANY (@boardIdentifiers::text[])) DESC
                    , ptibg.id
             LIMIT 1
         ) placement ON TRUE

WHERE pt.workspace_id = @workspaceId
  AND (@includeDeleted OR NOT pt.is_deleted)
  AND pt.id > @afterId
  AND (CARDINALITY(@projectKeys::text[]) = 0 OR LOWER(p.key) = ANY (@projectKeys::text[]))
  AND (CARDINALITY(@boardIdentifiers::text[]) = 0 OR EXISTS (
      SELECT 1
      FROM project_task_in_board_groups ptibg_filter
               INNER JOIN board_groups bg_filter ON ptibg_filter.board_group_id = bg_filter.id AND NOT bg_filter.is_deleted
               INNER JOIN boards b_filter ON bg_filter.board_id = b_filter.id
      WHERE ptibg_filter.project_task_id = pt.id
        AND LOWER(b_filter.identifier) = ANY (@boardIdentifiers::text[])
  ))
  AND (CARDINALITY(@statusKeys::text[]) = 0 OR LOWER(st.key) = ANY (@statusKeys::text[]))
  AND (CARDINALITY(@statusCategories::int[]) = 0 OR st.category = ANY (@statusCategories::int[]))
  AND (CARDINALITY(@priorities::int[]) = 0 OR pt.priority = ANY (@priorities::int[]))
  AND (@sprintId::int IS NULL OR pt.sprint_id = @sprintId::int)
  AND (CARDINALITY(@tags::text[]) = 0 OR EXISTS (
      SELECT 1
      FROM project_task_tags ptt_filter
               INNER JOIN tags t_filter ON ptt_filter.tag_id = t_filter.id AND NOT t_filter.is_deleted
      WHERE ptt_filter.project_task_id = pt.id
        AND LOWER(t_filter.name) = ANY (@tags::text[])
  ))
  AND (CARDINALITY(@assigneeEmails::text[]) = 0 OR EXISTS (
      SELECT 1
      FROM project_task_app_users ptau_filter
               INNER JOIN users u_filter ON ptau_filter.user_id = u_filter.id
      WHERE ptau_filter.project_task_id = pt.id
        AND LOWER(u_filter.email) = ANY (@assigneeEmails::text[])
  ))
  AND (@term = '' OR LOWER(pt.name) LIKE @termPattern OR LOWER(COALESCE(pt.description, '')) LIKE @termPattern)
  AND (@createdFrom::timestamptz IS NULL OR pt.created_at >= @createdFrom::timestamptz)
  AND (@createdTo::timestamptz IS NULL OR pt.created_at <= @createdTo::timestamptz)
  AND (@updatedSince::timestamptz IS NULL OR COALESCE(pt.updated_at, pt.created_at) >= @updatedSince::timestamptz)

ORDER BY pt.id
LIMIT @take;
