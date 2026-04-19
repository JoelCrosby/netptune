import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { refreshTokenSuccess, logout } from '@core/auth/store/auth.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, first, switchMap } from 'rxjs/operators';
import { WorkspaceService } from '../services/workspace.service';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private store = inject(Store);
  private router = inject(Router);
  private workspaceService = inject(WorkspaceService);
  private authService = inject(AuthService);

  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  intercept<T>(
    req: HttpRequest<T>,
    next: HttpHandler
  ): Observable<HttpEvent<T>> {
    if (!this.isApiRequest(req)) {
      return next.handle(req);
    }

    return this.store.select(selectCurrentWorkspaceIdentifier).pipe(
      first(),
      switchMap((workspaceId) => {
        req = req.clone({
          url: environment.apiEndpoint + req.url,
          withCredentials: true,
        });

        const workspaceRoute = this.workspaceService.getWorkspaceRoute();
        const workspaceHeader = workspaceId ?? workspaceRoute;

        if (workspaceHeader) {
          req = req.clone({
            headers: req.headers.set('workspace', workspaceHeader),
          });
        }

        return next.handle(req).pipe(
          catchError((err: unknown) => {
            if (err instanceof HttpErrorResponse) {
              if (err.status === 401 && !this.isRefreshRequest(req)) {
                return this.handle401(req, next);
              }

              if (err.status === 403) {
                void this.router.navigate(['/auth/login']);
              }
            }

            return throwError(() => err);
          })
        );
      })
    );
  }

  private handle401<T>(
    req: HttpRequest<T>,
    next: HttpHandler
  ): Observable<HttpEvent<T>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refresh().pipe(
        switchMap((user) => {
          this.isRefreshing = false;
          this.refreshTokenSubject.next('ok');
          this.store.dispatch(refreshTokenSuccess({ user }));
          return next.handle(req);
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.store.dispatch(logout({ silent: true }));
          return throwError(() => err);
        })
      );
    }

    return this.refreshTokenSubject.pipe(
      filter((token): token is string => token !== null),
      first(),
      switchMap(() => next.handle(req))
    );
  }

  private isRefreshRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.includes('auth/refresh');
  }

  isApiRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.startsWith('api/');
  }
}
