import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';

export const automationsManageGuard: CanActivateFn = (route) => {
  const store = inject(Store);
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);
  const id = route.params['id'];

  return store
    .select(selectHasPermission(netptunePermissions.automations.manage))
    .pipe(
      first(),
      map((canManage) => {
        if (canManage) return true;

        return router.createUrlTree(
          workspace && id
            ? ['/', workspace, 'automations', id]
            : workspace
              ? ['/', workspace, 'automations']
              : ['/']
        );
      })
    );
};
