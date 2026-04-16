import { Permission } from '../auth/permissions';
import { WorkspaceRole } from '../enums/workspace-role';

export interface UserPermissions {
  userId: string;
  workspaceKey: string;
  role: WorkspaceRole;
  permissions: Permission[];
}
