import { inject } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import { Store } from '@ngrx/store';
import { Observable, of, throwError } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';

export const workspaceResovler: ResolveFn<Workspace> = (
  next: ActivatedRouteSnapshot
): Observable<Workspace> => {
  const store = inject(Store);
  const currentWorkspace = inject(CurrentWorkspaceService);
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
        const open = currentWorkspace.workspace();

        if (open?.slug === workspaceKey) return of(open);

        return workspaces
          .getBySlug(workspaceKey)
          .pipe(tap((workspace) => currentWorkspace.set(workspace)));
      }

      return workspaces
        .getPublicBySlug(workspaceKey)
        .pipe(tap((workspace) => currentWorkspace.set(workspace)));
    })
  );
};
