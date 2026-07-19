import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, first, map, switchMap } from 'rxjs/operators';
import { currentUser } from '../store/auth/auth.actions';
import { setCurrentWorkspace } from '../store/workspaces/workspaces.actions';
import { WorkspacesService } from '../store/workspaces/workspaces.service';
import { WorkspaceService } from '../services/workspace.service';
import { AuthService } from './auth.service';

export const workspaceGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);
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
            store.dispatch(setCurrentWorkspace({ workspace }));

            return auth.currentUser().pipe(
              map((user) => {
                store.dispatch(currentUser.success({ user }));
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
          if (workspace?.isPublic) return true;
          return router.createUrlTree(['/auth/login']);
        }),
        catchError(() => of(router.createUrlTree(['/auth/login'])))
      );
    })
  );
};
