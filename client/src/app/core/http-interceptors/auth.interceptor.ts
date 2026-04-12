import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';
import { selectAuthTokenWithWorkspaceId } from '../auth/store/auth.selectors';
import { WorkspaceService } from '../services/workspace.service';
import { Logger } from '../util/logger';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private store = inject(Store);
  private router = inject(Router);
  private workspaceService = inject(WorkspaceService);

  intercept<T>(
    req: HttpRequest<T>,
    next: HttpHandler
  ): Observable<HttpEvent<T>> {
    return this.store.select(selectAuthTokenWithWorkspaceId).pipe(
      first(),
      switchMap(({ token, workspaceId }) => {
        if (!this.isApiRequest(req)) {
          Logger.warn('NOT API: ', req.url);

          return next.handle(req);
        }

        req = req.clone({
          url: environment.apiEndpoint + req.url,
        });

        const workspaceRoute = this.workspaceService.getWorkspaceRoute();
        const workspaceHeader = workspaceId ?? workspaceRoute;

        console.log({ workspaceRoute, workspaceHeader });

        if (token) {
          req = req.clone({
            headers: req.headers.set('Authorization', 'Bearer ' + token),
          });

          if (workspaceHeader) {
            req = req.clone({
              headers: req.headers.set('workspace', workspaceHeader),
            });
          }
        }

        return next.handle(req).pipe(
          tap({
            error: (err: unknown) => {
              if (err instanceof HttpErrorResponse) {
                if (err.status !== 403) {
                  return;
                }

                void this.router.navigate(['/auth/login']);
              }
            },
          })
        );
      })
    );
  }

  isApiRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.startsWith('api/');
  }
}
