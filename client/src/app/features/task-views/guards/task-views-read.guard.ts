import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';

export const taskViewsReadGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  const allowed = hasPermission(PERMISSIONS.taskViews.read)();

  return (
    allowed ||
    router.createUrlTree(workspace ? ['/', workspace, 'tasks'] : ['/'])
  );
};
