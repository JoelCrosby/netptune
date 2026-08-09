-- Grants assistant.use_web to existing members.
-- Permissions are snapshotted onto workspace_app_users when a member joins or their role changes,
-- so a new entry in WorkspaceRolePermissions reaches nobody who is already in a workspace.
-- Member and above, matching the code default; viewers are left without it.

UPDATE workspace_app_users
SET permissions = permissions || '["assistant.use_web"]'::jsonb
WHERE role >= 10
  AND NOT permissions @> '["assistant.use_web"]'::jsonb;
