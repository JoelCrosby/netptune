import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { setCurrentWorkspace } from '@core/store/workspaces/workspaces.actions';
import { Store } from '@ngrx/store';
import { Observable, throwError } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';

export const workspaceResovler: ResolveFn<Workspace> = (
  next: ActivatedRouteSnapshot
): Observable<Workspace> => {
  const http = inject(HttpClient);
  const store = inject(Store);

  const workspaceKey = next.paramMap.get('workspace');

  if (!workspaceKey) {
    return throwError(() => new Error('workspace key null'));
  }

  return store.select(selectIsAuthenticated).pipe(
    first(),
    switchMap((isAuthenticated) => {
      const url = isAuthenticated
        ? `api/workspaces/${workspaceKey}`
        : `api/public/workspaces/${workspaceKey}`;

      return http
        .get<Workspace>(url)
        .pipe(
          tap((workspace) => store.dispatch(setCurrentWorkspace({ workspace })))
        );
    })
  );
};
