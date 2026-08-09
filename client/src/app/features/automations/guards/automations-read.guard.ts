import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CanActivateFn, Router } from '@angular/router';
import { PERMISSONS } from '@core/auth/permissions';

export const automationsReadGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  const allowed = hasPermission(PERMISSONS.automations.read)();

  return (
    allowed ||
    router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/'])
  );
};
