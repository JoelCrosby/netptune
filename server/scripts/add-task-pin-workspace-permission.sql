-- Grants tasks.pin_workspace to existing admins and owners.
-- Permissions are snapshotted onto workspace_app_users when a member joins or their role changes,
-- so a new entry in WorkspaceRolePermissions reaches nobody who is already in a workspace.
-- Admin and above, matching the code default; members and viewers are left without it.

UPDATE workspace_app_users
SET permissions = permissions || '["tasks.pin_workspace"]'::jsonb
WHERE role >= 100
  AND NOT permissions @> '["tasks.pin_workspace"]'::jsonb;
