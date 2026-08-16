import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CanActivateFn, Router } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';

export const tasksRestoreGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  const allowed = hasPermission(PERMISSIONS.tasks.restore)();

  return (
    allowed ||
    router.createUrlTree(workspace ? ['/', workspace, 'tasks'] : ['/'])
  );
};
