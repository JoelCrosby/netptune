import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { logout, refreshTokenSuccess } from '@app/core/store/auth/auth.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, first, switchMap } from 'rxjs/operators';
import { WorkspaceService } from '../services/workspace.service';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

export const authInterceptor = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const store = inject(Store);
  const router = inject(Router);
  const workspaceService = inject(WorkspaceService);
  const authService = inject(AuthService);
  const refreshTokenSubject = new BehaviorSubject<string | null>(null);

  let isRefreshing = false;

  const isRefreshRequest = (req: HttpRequest<unknown>): boolean => {
    return req.url.includes('auth/refresh');
  };

  const isApiRequest = (req: HttpRequest<unknown>): boolean => {
    return req.url.startsWith('api/');
  };

  const handle401 = (req: HttpRequest<unknown>) => {
    if (!isRefreshing) {
      isRefreshing = true;
      refreshTokenSubject.next(null);

      return authService.refresh().pipe(
        switchMap((user) => {
          isRefreshing = false;
          refreshTokenSubject.next('ok');
          store.dispatch(refreshTokenSuccess({ user }));
          return next(req);
        }),
        catchError((err) => {
          isRefreshing = false;
          store.dispatch(logout({ silent: true }));
          return throwError(() => err);
        })
      );
    }

    return refreshTokenSubject.pipe(
      filter((token): token is string => token !== null),
      first(),
      switchMap(() => next(req))
    );
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
            if (err.status === 401 && !isRefreshRequest(req)) {
              return handle401(req);
            }

            if (err.status === 403) {
              void router.navigate(['/auth/login']);
            }
          }

          return throwError(() => err);
        })
      );
    })
  );
};
