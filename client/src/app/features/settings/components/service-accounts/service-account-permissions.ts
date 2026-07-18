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
