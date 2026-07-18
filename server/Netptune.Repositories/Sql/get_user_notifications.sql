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
        , al.actor_user_id  AS actoruserid
        , TRIM(u.firstname || ' ' || u.lastname) AS actorusername
        , u.picture_url     AS actorPictureUrl
        , n.activity_entry_id AS activityentryid
        , ae.changed_fields   AS changedfieldsarray
        , ae.revision_count   AS revisioncount
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
    -- LEFT, because discrete events and everything predating merging announce no entry.
    LEFT JOIN activity_entries ae ON ae.id = n.activity_entry_id
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
SELECT *
FROM notification_feed
WHERE @search::text IS NULL
   OR actorusername ILIKE @search
   OR entityname ILIKE @search
   OR entityidentifier ILIKE @search
ORDER BY createdat DESC
OFFSET @skip LIMIT @take;
