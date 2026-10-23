import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { reloadToLogin } from '@core/auth/reload-to-login';
import {
  SessionRefreshService,
  isSessionRejection,
} from '@core/auth/session-refresh.service';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { LoginResponse } from '@core/models/session';
import { environment } from '@env/environment';
import { from, Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { WorkspaceService } from '../services/workspace.service';
import { RealtimeClientIdService } from '../sse/realtime-client-id.service';

let sessionRefresh: Promise<LoginResponse> | null = null;

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
  const workspaceService = inject(WorkspaceService);
  const refresher = inject(SessionRefreshService);
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

  const refreshSession = (): Promise<LoginResponse> => {
    return refresher
      .refresh()
      .then((user) => {
        session.establish(user);
        workspaceList.reload();

        return user;
      })
      .catch((err: unknown) => {
        if (isSessionRejection(err)) {
          authCommands.endSession();
          reloadToLogin();
        }

        throw err;
      })
      .finally(() => {
        sessionRefresh = null;
      });
  };

  const handle401 = (req: HttpRequest<unknown>) => {
    sessionRefresh ??= refreshSession();

    return from(sessionRefresh).pipe(switchMap(() => next(req)));
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
