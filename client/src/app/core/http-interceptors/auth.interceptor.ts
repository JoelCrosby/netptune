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

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private store = inject(Store);
  private router = inject(Router);

  intercept<T>(
    req: HttpRequest<T>,
    next: HttpHandler
  ): Observable<HttpEvent<T>> {
    return this.store.select(selectAuthTokenWithWorkspaceId).pipe(
      first(),
      switchMap(({ token, workspaceId }) => {
        if (!this.isApiRequest(req)) {
          console.log('NOT API: ', req.url);

          return next.handle(req);
        }

        req = req.clone({
          url: environment.apiEndpoint + req.url,
        });

        const workspaceRoute = this.getWorkspaceRoute();
        const workspaceHeader = workspaceId ?? workspaceRoute;

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

  getWorkspaceRoute(): string | null {
    const url = window.location.pathname;
    const parts = url.split('/').filter((p) => !!p);

    if (parts.length >= 1) {
      const workspace = parts[0];

      if (workspace !== 'workspaces') {
        return workspace;
      }
    }

    return null;
  }
}
