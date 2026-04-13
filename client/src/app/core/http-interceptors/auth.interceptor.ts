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
import { refreshTokenSuccess } from '@core/auth/store/auth.actions';
import { selectAuthTokenWithWorkspaceId } from '@core/auth/store/auth.selectors';
import { UserToken } from '@core/auth/store/auth.models';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, first, switchMap } from 'rxjs/operators';
import { logout } from '../auth/store/auth.actions';
import { WorkspaceService } from '../services/workspace.service';

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
    return this.store.select(selectAuthTokenWithWorkspaceId).pipe(
      first(),
      switchMap(({ token, refreshToken, workspaceId }) => {
        if (!this.isApiRequest(req)) {
          return next.handle(req);
        }

        req = req.clone({
          url: environment.apiEndpoint + req.url,
        });

        const workspaceRoute = this.workspaceService.getWorkspaceRoute();
        const workspaceHeader = workspaceId ?? workspaceRoute;

        if (token) {
          req = this.cloneWithToken(req, token, workspaceHeader);
        } else if (workspaceHeader) {
          req = req.clone({
            headers: req.headers.set('workspace', workspaceHeader),
          });
        }

        return next.handle(req).pipe(
          catchError((err: unknown) => {
            if (err instanceof HttpErrorResponse) {
              if (err.status === 401 && !this.isRefreshRequest(req)) {
                return this.handle401(req, next, refreshToken);
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
    next: HttpHandler,
    refreshToken: string | undefined
  ): Observable<HttpEvent<T>> {
    if (!refreshToken) {
      this.store.dispatch(logout({ silent: true }));
      return throwError(() => new Error('No refresh token available'));
    }

    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refresh(refreshToken).pipe(
        switchMap((token: UserToken) => {
          this.isRefreshing = false;
          this.refreshTokenSubject.next(token.token);
          this.store.dispatch(refreshTokenSuccess({ token }));
          return next.handle(this.cloneWithToken(req, token.token));
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
      switchMap((token) => next.handle(this.cloneWithToken(req, token)))
    );
  }

  private cloneWithToken<T>(
    req: HttpRequest<T>,
    token: string,
    workspaceHeader?: string | null
  ): HttpRequest<T> {
    req = req.clone({
      headers: req.headers.set('Authorization', 'Bearer ' + token),
    });

    if (workspaceHeader) {
      req = req.clone({
        headers: req.headers.set('workspace', workspaceHeader),
      });
    }

    return req;
  }

  private isRefreshRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.includes('auth/refresh');
  }

  isApiRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.startsWith('api/');
  }
}
