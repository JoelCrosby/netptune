import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CanActivateFn, Router } from '@angular/router';
import { PERMISSONS } from '@core/auth/permissions';
import { SessionService } from '@core/services/session.service';

export const dashboardReadGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const isAuthenticated = inject(SessionService).isAuthenticated();
  const canRead = hasPermission(PERMISSONS.tasks.read)();

  if (isAuthenticated && canRead) return true;

  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean) as string | undefined;

  return router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/']);
};
