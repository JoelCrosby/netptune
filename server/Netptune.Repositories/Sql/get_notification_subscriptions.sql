-- A user's per-scope notification subscriptions in one workspace, with the name of the thing they
-- subscribed to. Scope discriminators arrive as Dapper parameters. Names are resolved from the live
-- rows rather than stored, so a rename shows through; a subscription whose target has been deleted
-- resolves to no name and drops out, matching the matcher, which stops delivering for it.
SELECT
      ns.id              AS id
    , ns.scope           AS scope
    , ns.scope_entity_id AS scopeentityid
    , ns.events          AS events
    , COALESCE(p.name, b.name, bg.name, s.name) AS name
    , CASE
        WHEN ns.scope = @boardScope      THEN bp.name
        WHEN ns.scope = @boardGroupScope THEN gb.name
        WHEN ns.scope = @sprintScope     THEN sp.name
      END AS context
    , CASE
        WHEN ns.scope = @projectScope    THEN p.key
        WHEN ns.scope = @boardScope      THEN b.identifier
        WHEN ns.scope = @boardGroupScope THEN gb.identifier
        WHEN ns.scope = @sprintScope     THEN s.id::text
      END AS linkidentifier
FROM notification_subscriptions ns
LEFT JOIN projects p      ON ns.scope = @projectScope    AND p.id  = ns.scope_entity_id AND NOT p.is_deleted
LEFT JOIN boards b        ON ns.scope = @boardScope      AND b.id  = ns.scope_entity_id AND NOT b.is_deleted
LEFT JOIN projects bp     ON bp.id = b.project_id
LEFT JOIN board_groups bg ON ns.scope = @boardGroupScope AND bg.id = ns.scope_entity_id AND NOT bg.is_deleted
LEFT JOIN boards gb       ON gb.id = bg.board_id AND NOT gb.is_deleted
LEFT JOIN sprints s       ON ns.scope = @sprintScope     AND s.id  = ns.scope_entity_id AND NOT s.is_deleted
LEFT JOIN projects sp     ON sp.id = s.project_id
WHERE ns.user_id = @userId
  AND ns.workspace_id = @workspaceId
  AND NOT ns.is_deleted
  AND COALESCE(p.name, b.name, bg.name, s.name) IS NOT NULL
  AND (ns.scope <> @boardGroupScope OR gb.id IS NOT NULL)
ORDER BY ns.scope, name;
