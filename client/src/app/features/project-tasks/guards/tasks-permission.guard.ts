import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { Permission } from '@core/auth/permissions';

export function tasksPermissionGuard(permission: Permission): CanActivateFn {
  return (route) => {
    const router = inject(Router);
    const workspace = route.pathFromRoot
      .map((snapshot) => snapshot.params['workspace'])
      .find(Boolean);

    const allowed = hasPermission(permission)();

    return (
      allowed ||
      router.createUrlTree(workspace ? ['/', workspace, 'tasks'] : ['/'])
    );
  };
}
