import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';

export const projectDetailGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);
  const router = inject(Router);
  const projectKey = route.params?.['id'];

  if (!projectKey) return false;

  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  return store
    .select(selectHasPermission(netptunePermissions.projects.update))
    .pipe(
      first(),
      map((canUpdate) => {
        if (!canUpdate) {
          return router.createUrlTree(
            workspace ? ['/', workspace, 'projects'] : ['/']
          );
        }

        return true;
      })
    );
};
