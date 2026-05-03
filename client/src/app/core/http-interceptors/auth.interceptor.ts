import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import {
  logoutSuccess,
  refreshTokenSuccess,
} from '@app/core/store/auth/auth.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable, throwError } from 'rxjs';
import {
  catchError,
  finalize,
  first,
  shareReplay,
  switchMap,
  tap,
} from 'rxjs/operators';
import { WorkspaceService } from '../services/workspace.service';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

let sessionRefreshRequest$: ReturnType<AuthService['refresh']> | null = null;

export const authInterceptor = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const store = inject(Store);
  const router = inject(Router);
  const workspaceService = inject(WorkspaceService);
  const authService = inject(AuthService);

  const isAuthManagementRequest = (req: HttpRequest<unknown>): boolean => {
    return (
      req.url.includes('api/auth/refresh') ||
      req.url.includes('api/auth/logout')
    );
  };

  const isApiRequest = (req: HttpRequest<unknown>): boolean => {
    return req.url.startsWith('api/');
  };

  const handle401 = (req: HttpRequest<unknown>) => {
    if (!sessionRefreshRequest$) {
      sessionRefreshRequest$ = authService.refresh().pipe(
        tap((user) => store.dispatch(refreshTokenSuccess({ user }))),
        finalize(() => {
          sessionRefreshRequest$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    const sessionRefreshWithLogoutOnFailure$ = sessionRefreshRequest$.pipe(
      catchError((err) => {
        store.dispatch(logoutSuccess());
        void router.navigate(['/auth/login']);
        return throwError(() => err);
      })
    );

    return sessionRefreshWithLogoutOnFailure$.pipe(switchMap(() => next(req)));
  };

  if (!isApiRequest(req)) {
    return next(req);
  }

  return store.select(selectCurrentWorkspaceIdentifier).pipe(
    first(),
    switchMap((workspaceId) => {
      req = req.clone({
        url: environment.apiEndpoint + req.url,
        withCredentials: true,
      });

      const workspaceRoute = workspaceService.getWorkspaceRoute();
      const workspaceHeader = workspaceId ?? workspaceRoute;

      if (workspaceHeader) {
        req = req.clone({
          headers: req.headers.set('workspace', workspaceHeader),
        });
      }

      return next(req).pipe(
        catchError((err: unknown) => {
          if (err instanceof HttpErrorResponse) {
            if (err.status === 401 && !isAuthManagementRequest(req)) {
              return handle401(req);
            }
          }

          return throwError(() => err);
        })
      );
    })
  );
};
