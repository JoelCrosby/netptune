-- Paged workspace people for UserRepository.GetWorkspaceUsersPaged.
-- Merges active members (workspace_app_users + users) with pending invites
-- (workspace_invites) that are not already members, into one sorted, paginated set.
-- Members always sort ahead of pending invites. The chosen sort key ('user' or
-- 'email') applies to members; pending invites always sort by email.
-- count(*) OVER () carries the unpaged total on every row.
-- Named parameters:
--   @workspace_id    workspace id
--   @limit           page size
--   @offset          rows to skip
--   @sort_by         'user' | 'email' | anything else => firstname/lastname/id
--   @sort_direction  'desc' => descending (members only), otherwise ascending
WITH combined AS (
    SELECT
        u.id                AS id,
        u.firstname         AS firstname,
        u.lastname          AS lastname,
        u.picture_url       AS pictureurl,
        CASE
            WHEN btrim(coalesce(u.firstname, '')) = '' AND btrim(coalesce(u.lastname, '')) = ''
                THEN u.user_name
            ELSE concat(u.firstname, ' ', u.lastname)
        END                 AS displayname,
        u.email             AS email,
        u.user_name         AS username,
        u.last_login_time   AS lastlogintime,
        u.registration_date AS registrationdate,
        wau.role            AS role,
        FALSE               AS ispending,
        u.normalized_email  AS sort_email,
        wau.user_id         AS sort_id
    FROM workspace_app_users wau
    JOIN users u ON u.id = wau.user_id
    WHERE wau.workspace_id = @workspace_id
      AND u.user_type = 0

    UNION ALL

    SELECT
        NULL                AS id,
        NULL                AS firstname,
        NULL                AS lastname,
        NULL                AS pictureurl,
        wi.email            AS displayname,
        wi.email            AS email,
        NULL                AS username,
        NULL                AS lastlogintime,
        NULL                AS registrationdate,
        'member'::workspace_role AS role,       -- WorkspaceRole.Member
        TRUE                AS ispending,
        NULL                AS sort_email,
        NULL                AS sort_id
    FROM workspace_invites wi
    WHERE wi.workspace_id = @workspace_id
      AND wi.accepted_at IS NULL
      AND NOT EXISTS (
          SELECT 1
          FROM workspace_app_users m
          JOIN users mu ON mu.id = m.user_id
          WHERE m.workspace_id = @workspace_id
            AND mu.normalized_email = upper(wi.email)
      )
)
SELECT
    id,
    firstname,
    lastname,
    pictureurl,
    displayname,
    email,
    username,
    lastlogintime,
    registrationdate,
    role,
    ispending,
    count(*) OVER () AS totalcount
FROM combined
ORDER BY
    ispending ASC,
    CASE WHEN @sort_by = 'user'  AND @sort_direction <> 'desc' THEN firstname END ASC,
    CASE WHEN @sort_by = 'user'  AND @sort_direction =  'desc' THEN firstname END DESC,
    CASE WHEN @sort_by = 'email' AND @sort_direction <> 'desc' THEN sort_email END ASC,
    CASE WHEN @sort_by = 'email' AND @sort_direction =  'desc' THEN sort_email END DESC,
    CASE WHEN @sort_by NOT IN ('user', 'email') THEN firstname END ASC,
    CASE WHEN @sort_by NOT IN ('user', 'email') THEN lastname  END ASC,
    CASE WHEN @sort_by NOT IN ('user', 'email') THEN sort_id   END ASC,
    email ASC
LIMIT @limit
OFFSET @offset
