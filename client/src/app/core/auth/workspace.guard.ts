import { inject } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, first, map, switchMap } from 'rxjs/operators';
import { currentUserLoaded } from '../store/auth/auth.actions';
import { WorkspacesService } from '../services/workspaces-api.service';
import { WorkspaceService } from '../services/workspace.service';
import { AuthService } from './auth.service';

export const workspaceGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);
  const currentWorkspace = inject(CurrentWorkspaceService);
  const router = inject(Router);
  const auth = inject(AuthService);
  const workspaces = inject(WorkspacesService);
  const workspaceService = inject(WorkspaceService);

  const workspaceKey =
    route.paramMap.get('workspace') ?? route.parent?.paramMap.get('workspace');

  return store.select(selectIsAuthenticated).pipe(
    first(),
    switchMap((isAuthenticated) => {
      if (!workspaceKey) {
        return of(router.createUrlTree(['/auth/login']));
      }

      if (isAuthenticated) {
        const previousWorkspace = workspaceService.currentWorkspace();
        workspaceService.setWorkspace(workspaceKey);

        return workspaces.getBySlug(workspaceKey).pipe(
          switchMap((workspace) => {
            currentWorkspace.set(workspace);

            return auth.currentUser().pipe(
              map((user) => {
                store.dispatch(currentUserLoaded({ user }));
                return true;
              })
            );
          }),
          catchError(() => {
            workspaceService.setWorkspace(previousWorkspace);

            return of(router.createUrlTree(['/auth/login']));
          })
        );
      }

      return workspaces.getPublicBySlug(workspaceKey).pipe(
        map((workspace) => {
          if (!workspace?.isPublic) {
            return router.createUrlTree(['/auth/login']);
          }

          workspaceService.setWorkspace(workspaceKey);
          currentWorkspace.set(workspace);

          return true;
        }),
        catchError(() => of(router.createUrlTree(['/auth/login'])))
      );
    })
  );
};
