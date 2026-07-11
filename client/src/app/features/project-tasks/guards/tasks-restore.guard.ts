import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';

export const tasksRestoreGuard: CanActivateFn = (route) => {
  const store = inject(Store);
  const router = inject(Router);
  const workspace = route.pathFromRoot
    .map((snapshot) => snapshot.params['workspace'])
    .find(Boolean);

  return store
    .select(selectHasPermission(netptunePermissions.tasks.restore))
    .pipe(
      first(),
      map(
        (canRestore) =>
          canRestore ||
          router.createUrlTree(workspace ? ['/', workspace, 'tasks'] : ['/'])
      )
    );
};
