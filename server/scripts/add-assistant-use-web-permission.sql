-- Grants assistant.use_web to existing members.
-- Permissions are snapshotted onto workspace_app_users when a member joins or their role changes,
-- so a new entry in WorkspaceRolePermissions reaches nobody who is already in a workspace.
-- Member and above, matching the code default; viewers are left without it.
-- The role column is the workspace_role enum, whose sort order is alphabetical
-- (admin, member, owner, viewer), so the roles have to be listed rather than compared with >=.

UPDATE workspace_app_users
SET permissions = permissions || '["assistant.use_web"]'::jsonb
WHERE role IN ('member', 'admin', 'owner')
  AND NOT permissions @> '["assistant.use_web"]'::jsonb;
