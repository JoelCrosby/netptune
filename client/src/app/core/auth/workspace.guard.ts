import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, first, map, switchMap } from 'rxjs/operators';

export const workspaceGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const store = inject(Store);
  const http = inject(HttpClient);
  const router = inject(Router);

  const workspaceKey = route.paramMap.get('workspace');

  return store.select(selectIsAuthenticated).pipe(
    first(),
    switchMap((isAuthenticated) => {
      if (isAuthenticated) return of(true);

      if (!workspaceKey) {
        void router.navigate(['/auth/login']);
        return of(false);
      }

      return http.get<Workspace>(`api/public/workspaces/${workspaceKey}`).pipe(
        map((workspace) => {
          if (workspace?.isPublic) return true;
          void router.navigate(['/auth/login']);
          return false;
        }),
        catchError(() => {
          void router.navigate(['/auth/login']);
          return of(false);
        })
      );
    })
  );
};
