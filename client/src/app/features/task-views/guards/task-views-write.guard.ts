import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';

export const taskViewsWriteGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  const canCreate = hasPermission(PERMISSIONS.taskViews.create)();
  const canUpdate = hasPermission(PERMISSIONS.taskViews.update)();
  const allowed = canCreate || canUpdate;

  return (
    allowed ||
    router.createUrlTree(workspace ? ['/', workspace, 'views'] : ['/'])
  );
};
