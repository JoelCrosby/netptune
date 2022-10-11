import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AppState } from '@core/core.state';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { combineLatest, Observable } from 'rxjs';
import { first, switchMap, tap } from 'rxjs/operators';
import { selectAuthToken } from '../auth/store/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private store: Store<AppState>, private router: Router) {}

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

        if (token) {
          req = req.clone({
            headers: req.headers.set('Authorization', 'Bearer ' + token),
          });

          if (workspace) {
            req = req.clone({
              headers: req.headers.set('workspace', workspace),
            });
          }
        }

        return next.handle(req).pipe(
          tap({
            error: (err: unknown) => {
              if (err instanceof HttpErrorResponse) {
                if (err.status !== 401) {
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
}
