import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { PERMISSONS } from '@core/auth/permissions';

export const projectDetailGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const router = inject(Router);
  const projectKey = route.params?.['id'];

  if (!projectKey) return false;

  const canUpdate = hasPermission(PERMISSONS.projects.update)();

  if (canUpdate) return true;

  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  return router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/']);
};
