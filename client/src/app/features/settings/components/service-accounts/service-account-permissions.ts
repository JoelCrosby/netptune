import {
  netptunePermissionLabels,
  PermissionMeta,
} from '@core/auth/permission-items';
import { Permission } from '@core/auth/permissions';

const labels = Object.values(netptunePermissionLabels)
  .flatMap((group) => Object.values(group) as PermissionMeta[])
  .reduce(
    (result, permission) => result.set(permission.key, permission.label),
    new Map<string, string>()
  );

export function permissionLabel(permission: Permission): string {
  return labels.get(permission) ?? permission;
}

export interface PermissionGroupOption {
  key: string;
  label: string;
  permissions: PermissionOption[];
}

export interface PermissionOption {
  key: Permission;
  label: string;
}

const groupLabels: Record<string, string> = {
  workspace: 'Workspace',
  members: 'Members',
  projects: 'Projects',
  boards: 'Boards',
  boardGroups: 'Board groups',
  tasks: 'Tasks',
  sprints: 'Sprints',
  comments: 'Comments',
  tags: 'Tags',
  statuses: 'Statuses',
  relationTypes: 'Relation types',
  activity: 'Activity',
  audit: 'Audit',
  notifications: 'Notifications',
  automations: 'Automations',
  flags: 'Flags',
  serviceAccounts: 'Service accounts',
  storage: 'Storage',
  files: 'Files',
};

export const permissionGroups: PermissionGroupOption[] = Object.entries(
  netptunePermissionLabels
).map(([group, permissions]) => ({
  key: group,
  label: groupLabels[group] ?? group,
  permissions: (Object.values(permissions) as PermissionMeta[]).map(
    (permission) => ({
      key: permission.key as Permission,
      label: permission.label,
    })
  ),
}));

export const allPermissions: Permission[] = permissionGroups.flatMap((group) =>
  group.permissions.map((permission) => permission.key)
);

export function filterPermissionGroups(
  available: Iterable<Permission>
): PermissionGroupOption[] {
  const allowed = new Set(available);

  return permissionGroups
    .map((group) => ({
      ...group,
      permissions: group.permissions.filter((permission) => {
        return allowed.has(permission.key);
      }),
    }))
    .filter((group) => group.permissions.length > 0);
}
