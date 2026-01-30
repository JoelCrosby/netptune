import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { setCurrentWorkspace } from '@core/store/workspaces/workspaces.actions';
import { Store } from '@ngrx/store';
import { Observable, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';

export const workspaceResovler: ResolveFn<Workspace> = (
  next: ActivatedRouteSnapshot
): Observable<Workspace> => {
  const http = inject(HttpClient);
  const store = inject(Store);

  const workspaceKey = next.paramMap.get('workspace');

  if (!workspaceKey) {
    return throwError(() => new Error('workspace key null'));
  }

  return http
    .get<Workspace>(`api/workspaces/${workspaceKey}`)
    .pipe(
      tap((workspace) => store.dispatch(setCurrentWorkspace({ workspace })))
    );
};
