import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CanActivateFn, Router } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';

export const automationsManageGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const canManage = hasPermission(PERMISSIONS.automations.manage)();

  if (canManage) return true;

  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);
  const id = route.params['id'];

  if (workspace && id) {
    return router.createUrlTree(['/', workspace, 'automations', id]);
  }

  return router.createUrlTree(
    workspace ? ['/', workspace, 'automations'] : ['/']
  );
};
