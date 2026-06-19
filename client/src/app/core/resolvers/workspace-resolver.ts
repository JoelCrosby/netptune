import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { setCurrentWorkspace } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { Store } from '@ngrx/store';
import { Observable, of, throwError } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';

export const workspaceResovler: ResolveFn<Workspace> = (
  next: ActivatedRouteSnapshot
): Observable<Workspace> => {
  const store = inject(Store);
  const workspaces = inject(WorkspacesService);

  const workspaceKey =
    next.paramMap.get('workspace') ?? next.parent?.paramMap.get('workspace');

  if (!workspaceKey) {
    return throwError(() => new Error('workspace key null'));
  }

  return store.select(selectIsAuthenticated).pipe(
    first(),
    switchMap((isAuthenticated) => {
      if (isAuthenticated) {
        return store.select(selectCurrentWorkspace).pipe(
          first(),
          switchMap((workspace) => {
            if (workspace?.slug === workspaceKey) return of(workspace);

            return workspaces.getBySlug(workspaceKey).pipe(
              tap((workspace) =>
                store.dispatch(setCurrentWorkspace({ workspace }))
              )
            );
          })
        );
      }

      return workspaces.getPublicBySlug(workspaceKey).pipe(
        tap((workspace) => store.dispatch(setCurrentWorkspace({ workspace })))
      );
    })
  );
};
