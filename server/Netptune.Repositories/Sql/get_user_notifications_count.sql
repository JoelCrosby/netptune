-- Total count for the paginated notification feed, used by
-- NotificationRepository.GetUserNotificationsCount. Mirrors the filtering of
-- get_user_notifications.sql (search + actor) so pagination totals stay in sync.
WITH notification_feed AS (
    SELECT
          n.id
        , TRIM(u.firstname || ' ' || u.lastname) AS actorusername
        , COALESCE(pt.name, p.name, b.name, bg.name, s.name) AS entityname
        , CASE
            WHEN n.entity_type = @taskType    AND tp.key IS NOT NULL THEN tp.key || '-' || pt.project_scope_id::text
            WHEN n.entity_type = @boardType   THEN b.identifier
            WHEN n.entity_type = @projectType THEN p.key
            WHEN n.entity_type = @statusType  THEN s.key
          END AS entityidentifier
    FROM notifications n
    INNER JOIN event_records al ON al.id = n.event_record_id
    INNER JOIN users u ON u.id = al.actor_user_id
    LEFT JOIN project_tasks pt  ON pt.id  = CASE WHEN al.subject_type = 'task' THEN al.subject_id::integer END AND n.entity_type = @taskType
    LEFT JOIN projects tp       ON tp.id = pt.project_id
    LEFT JOIN projects p        ON p.id   = CASE WHEN al.subject_type = 'project' THEN al.subject_id::integer END AND n.entity_type = @projectType
    LEFT JOIN boards b          ON b.id   = CASE WHEN al.subject_type = 'board' THEN al.subject_id::integer END AND n.entity_type = @boardType
    LEFT JOIN board_groups bg   ON bg.id  = CASE WHEN al.subject_type = 'boardgroup' THEN al.subject_id::integer END AND n.entity_type = @boardGroupType
    LEFT JOIN statuses s        ON s.id   = CASE WHEN al.subject_type = 'status' THEN al.subject_id::integer END AND n.entity_type = @statusType
    WHERE n.is_deleted = FALSE
      AND n.user_id = @userId
      AND n.workspace_id = @workspaceId
      AND (@actorId::text IS NULL OR al.actor_user_id = @actorId)
)
SELECT COUNT(*)
FROM notification_feed
WHERE @search::text IS NULL
   OR actorusername ILIKE @search
   OR entityname ILIKE @search
   OR entityidentifier ILIKE @search;
