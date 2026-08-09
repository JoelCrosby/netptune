import { inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import { Observable, of, throwError } from 'rxjs';
import { switchMap, tap } from 'rxjs/operators';

export const workspaceResovler: ResolveFn<Workspace> = (
  next: ActivatedRouteSnapshot
): Observable<Workspace> => {
  const currentWorkspace = inject(CurrentWorkspaceService);
  const session = inject(SessionService);
  const workspaces = inject(WorkspacesService);

  const workspaceKey =
    next.paramMap.get('workspace') ?? next.parent?.paramMap.get('workspace');

  if (!workspaceKey) {
    return throwError(() => new Error('workspace key null'));
  }

  return of(session.isAuthenticated()).pipe(
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
