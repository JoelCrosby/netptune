INSERT INTO activity_entries (
      workspace_id
    , workspace_slug
    , entity_type
    , entity_id
    , user_id
    , activity_type
    , changed_fields
    , meta
    , last_activity_log_id
    , first_occurred_at
    , last_occurred_at
    , revision_count
    , is_open
    , window_expires_at
    , notified_at
    , project_id
    , project_slug
    , board_id
    , board_slug
    , board_group_id
    , task_id
    , is_deleted
    , created_at
    , updated_at
    , created_by_user_id
    , owner_id
)
VALUES (
      @workspace_id
    , @workspace_slug::text
    , @entity_type
    , @entity_id
    , @user_id::text
    , @activity_type
    , @changed_fields::text[]
    , @meta::jsonb
    , @last_activity_log_id
    , @first_occurred_at
    , @last_occurred_at
    , @revision_count
    , TRUE
    , LEAST(
          @now + @window_seconds::double precision * INTERVAL '1 second'
        , @first_occurred_at + @max_window_seconds::double precision * INTERVAL '1 second')
    , NULL
    , @project_id::int
    , @project_slug::text
    , @board_id::int
    , @board_slug::text
    , @board_group_id::int
    , @task_id::int
    , FALSE
    , @now
    , @now
    , @user_id::text
    , @user_id::text
)
ON CONFLICT (workspace_id, entity_type, entity_id, user_id) WHERE $open_entry_index_filter$
DO UPDATE SET
      activity_type = CASE
          WHEN activity_entries.activity_type = EXCLUDED.activity_type THEN activity_entries.activity_type
          ELSE @merged_activity_type
      END

    , changed_fields = ARRAY(
          SELECT DISTINCT unnest(activity_entries.changed_fields || EXCLUDED.changed_fields)
          ORDER BY 1
      )

    , last_activity_log_id = EXCLUDED.last_activity_log_id

    , meta = (COALESCE(activity_entries.meta, jsonb_build_object()) || COALESCE(EXCLUDED.meta, jsonb_build_object()))
             || jsonb_build_object('fields', (
                 SELECT COALESCE(jsonb_object_agg(
                       COALESCE(existing.key, incoming.key)
                     , jsonb_build_object(
                           'old',     CASE WHEN existing.value ? 'old' THEN existing.value -> 'old'     ELSE incoming.value -> 'old' END
                         , 'oldHash', CASE WHEN existing.value ? 'old' THEN existing.value -> 'oldHash' ELSE incoming.value -> 'oldHash' END
                         , 'new',     CASE WHEN incoming.value ? 'new' THEN incoming.value -> 'new'     ELSE existing.value -> 'new' END
                         , 'newHash', CASE WHEN incoming.value ? 'new' THEN incoming.value -> 'newHash' ELSE existing.value -> 'newHash' END
                       )), jsonb_build_object())
                 FROM jsonb_each(COALESCE(activity_entries.meta -> 'fields', jsonb_build_object())) AS existing
                 FULL OUTER JOIN jsonb_each(COALESCE(EXCLUDED.meta -> 'fields', jsonb_build_object())) AS incoming
                   ON existing.key = incoming.key
             ))

    , first_occurred_at = LEAST(activity_entries.first_occurred_at, EXCLUDED.first_occurred_at)
    , last_occurred_at = GREATEST(activity_entries.last_occurred_at, EXCLUDED.last_occurred_at)
    , revision_count = activity_entries.revision_count + EXCLUDED.revision_count

    , window_expires_at = LEAST(
          @now + @window_seconds::double precision * INTERVAL '1 second'
        , LEAST(activity_entries.first_occurred_at, EXCLUDED.first_occurred_at) + @max_window_seconds::double precision * INTERVAL '1 second')

    , updated_at = @now
WHERE activity_entries.window_expires_at > @now
RETURNING *;
