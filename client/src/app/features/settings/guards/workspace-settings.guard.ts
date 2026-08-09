import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CanActivateFn, Router } from '@angular/router';
import { Permission } from '@core/auth/permissions';

export const workspaceSettingsGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const permission = route.data['permission'] as Permission;
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  const allowed = hasPermission(permission)();

  return (
    allowed ||
    router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/'])
  );
};
