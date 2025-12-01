import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { combineLatest, Observable } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';
import { selectAuthToken } from '../auth/store/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private store = inject(Store);
  private router = inject(Router);

  intercept<T>(
    req: HttpRequest<T>,
    next: HttpHandler
  ): Observable<HttpEvent<T>> {
    return combineLatest([
      this.store.select(selectAuthToken),
      this.store.select(selectCurrentWorkspaceIdentifier),
    ]).pipe(
      first(),
      switchMap(([token, workspace]) => {
        if (!this.isApiRequest(req)) {
          return next.handle(req);
        }

        const workspaceRoute = this.getWorkspaceRoute();
        const workspaceHeader = workspace ?? workspaceRoute;

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
    return req.url.startsWith(environment.apiEndpoint);
  }

  getWorkspaceRoute(): string | null {
    const url = window.location.pathname;
    const parts = url.split('/').filter((p) => !!p);

    if (parts.length === 1) {
      const workspace = parts[0];

      if (workspace !== 'workspaces') {
        return workspace;
      }
    }

    return null;
  }
}
