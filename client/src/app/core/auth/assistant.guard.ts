import { inject } from '@angular/core';
import { CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';
import { selectIsAssistantAvailable } from '../store/auth/auth.selectors';

export const assistantGuard: CanActivateFn = (
  _route,
  state: RouterStateSnapshot
) => {
  const store = inject(Store);
  const router = inject(Router);
  const workspaceKey = state.url.split('?')[0].split('/').filter(Boolean)[0];

  return store.select(selectIsAssistantAvailable).pipe(
    first(),
    map((isAvailable) => {
      if (isAvailable) {
        return true;
      }

      return router.createUrlTree(['/', workspaceKey]);
    })
  );
};
