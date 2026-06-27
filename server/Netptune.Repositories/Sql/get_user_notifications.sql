-- Paginated notification feed for a user within a workspace, used by
-- NotificationRepository.GetUserNotifications. Entity-type discriminators and
-- the @skip/@take window are supplied as Dapper parameters.
SELECT
      n.id
    , n.is_read         AS isread
    , n.link
    , n.entity_type     AS entitytype
    , n.activity_type   AS activitytype
    , n.created_at      AS createdat
    , al.user_id        AS actoruserid
    , TRIM(u.firstname || ' ' || u.lastname) AS actorusername
    , u.picture_url     AS actoruserurl
    , COALESCE(pt.name, p.name, b.name, bg.name, s.name) AS entityname
    , CASE
        WHEN n.entity_type = @taskType    AND al.project_slug IS NOT NULL THEN al.project_slug || '-' || pt.project_scope_id::text
        WHEN n.entity_type = @boardType   THEN al.board_slug
        WHEN n.entity_type = @projectType THEN al.project_slug
        WHEN n.entity_type = @statusType  THEN s.key
      END AS entityidentifier
FROM notifications n
INNER JOIN activity_logs al ON al.id = n.activity_log_id
INNER JOIN users u ON u.id = al.user_id
LEFT JOIN project_tasks pt  ON pt.id  = al.task_id         AND n.entity_type = @taskType
LEFT JOIN projects p        ON p.id   = al.project_id      AND n.entity_type = @projectType
LEFT JOIN boards b          ON b.id   = al.board_id        AND n.entity_type = @boardType
LEFT JOIN board_groups bg   ON bg.id  = al.board_group_id  AND n.entity_type = @boardGroupType
LEFT JOIN statuses s        ON s.id   = al.entity_id       AND n.entity_type = @statusType
WHERE n.is_deleted = FALSE
  AND n.user_id = @userId
  AND n.workspace_id = @workspaceId
ORDER BY n.created_at DESC
OFFSET @skip LIMIT @take;
