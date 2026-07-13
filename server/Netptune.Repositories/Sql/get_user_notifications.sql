-- Paginated notification feed for a user within a workspace, used by
-- NotificationRepository.GetUserNotifications. Entity-type discriminators and
-- the @skip/@take window are supplied as Dapper parameters. The optional
-- @search (pre-wrapped with wildcards) and @actorId parameters filter the feed
-- by text and by the acting user respectively.
WITH notification_feed AS (
    SELECT
          n.id
        , n.is_read         AS isread
        , n.link
        , n.entity_type     AS entitytype
        , n.activity_type   AS activitytype
        , n.created_at      AS createdat
        , al.user_id        AS actoruserid
        , TRIM(u.firstname || ' ' || u.lastname) AS actorusername
        , u.picture_url     AS actorPictureUrl
        , n.activity_entry_id AS activityentryid
        , ae.changed_fields   AS changedfieldsarray
        , ae.revision_count   AS revisioncount
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
    -- LEFT, because discrete events and everything predating merging announce no entry.
    LEFT JOIN activity_entries ae ON ae.id = n.activity_entry_id
    LEFT JOIN project_tasks pt  ON pt.id  = al.task_id         AND n.entity_type = @taskType
    LEFT JOIN projects p        ON p.id   = al.project_id      AND n.entity_type = @projectType
    LEFT JOIN boards b          ON b.id   = al.board_id        AND n.entity_type = @boardType
    LEFT JOIN board_groups bg   ON bg.id  = al.board_group_id  AND n.entity_type = @boardGroupType
    LEFT JOIN statuses s        ON s.id   = al.entity_id       AND n.entity_type = @statusType
    WHERE n.is_deleted = FALSE
      AND n.user_id = @userId
      AND n.workspace_id = @workspaceId
      AND (@actorId::text IS NULL OR al.user_id = @actorId)
)
SELECT *
FROM notification_feed
WHERE @search::text IS NULL
   OR actorusername ILIKE @search
   OR entityname ILIKE @search
   OR entityidentifier ILIKE @search
ORDER BY createdat DESC
OFFSET @skip LIMIT @take;
