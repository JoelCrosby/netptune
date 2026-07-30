import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import {
  selectHasPermission,
  selectIsAuthenticated,
} from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { combineLatest } from 'rxjs';
import { first, map } from 'rxjs/operators';

export const dashboardReadGuard: CanActivateFn = (route) => {
  const store = inject(Store);
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean) as string | undefined;

  return combineLatest([
    store.select(selectIsAuthenticated),
    store.select(selectHasPermission(netptunePermissions.tasks.read)),
  ]).pipe(
    first(),
    map(([isAuthenticated, canRead]) => {
      const hasAccess = isAuthenticated && canRead;

      return (
        hasAccess ||
        router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/'])
      );
    })
  );
};
