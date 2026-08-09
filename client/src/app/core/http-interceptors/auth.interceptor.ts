import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { environment } from '@env/environment';
import { Observable, throwError } from 'rxjs';
import {
  catchError,
  finalize,
  shareReplay,
  switchMap,
  tap,
} from 'rxjs/operators';
import { WorkspaceService } from '../services/workspace.service';
import { RealtimeClientIdService } from '../sse/realtime-client-id.service';

let sessionRefreshRequest$: ReturnType<AuthService['refresh']> | null = null;

export const resolveWorkspaceHeader = (
  workspaceRoute: string | null,
  selectedWorkspace: string | undefined
): string | undefined => workspaceRoute ?? selectedWorkspace;

export const authInterceptor = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authCommands = inject(AuthCommandsService);
  const currentWorkspace = inject(CurrentWorkspaceService);
  const session = inject(SessionService);
  const workspaceList = inject(WorkspaceListService);
  const router = inject(Router);
  const workspaceService = inject(WorkspaceService);
  const authService = inject(AuthService);
  const realtimeClientId = inject(RealtimeClientIdService);

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
        tap((user) => {
          session.establish(user);
          workspaceList.reload();
        }),
        finalize(() => {
          sessionRefreshRequest$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    const sessionRefreshWithLogoutOnFailure$ = sessionRefreshRequest$.pipe(
      catchError((err: unknown) => {
        authCommands.endSession();
        void router.navigate(['/auth/login']);
        return throwError(() => err);
      })
    );

    return sessionRefreshWithLogoutOnFailure$.pipe(switchMap(() => next(req)));
  };

  if (!isApiRequest(req)) {
    return next(req);
  }

  const workspaceId = currentWorkspace.slug();

  req = req.clone({
    headers: req.headers.set('X-Realtime-Client', realtimeClientId.value),
    url: environment.apiEndpoint + req.url,
    withCredentials: true,
  });

  const workspaceRoute = workspaceService.getWorkspaceRoute();
  const workspaceHeader = resolveWorkspaceHeader(workspaceRoute, workspaceId);

  if (workspaceHeader) {
    req = req.clone({
      headers: req.headers.set('workspace', workspaceHeader),
    });
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 401 && !isAuthManagementRequest(req)) {
          return session.hasAuthSession()
            ? handle401(req)
            : throwError(() => err);
        }
      }

      return throwError(() => err);
    })
  );
};
