import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Permission } from '@core/auth/permissions';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';

export const workspaceSettingsGuard: CanActivateFn = (route) => {
  const store = inject(Store);
  const router = inject(Router);
  const permission = route.data['permission'] as Permission;
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  return store.select(selectHasPermission(permission)).pipe(
    first(),
    map(
      (canRead) =>
        canRead ||
        router.createUrlTree(workspace ? ['/', workspace, 'projects'] : ['/'])
    )
  );
};
