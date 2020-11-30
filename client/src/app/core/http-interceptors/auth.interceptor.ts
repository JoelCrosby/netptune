import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { combineLatest, Observable } from 'rxjs';
import { first, switchMap } from 'rxjs/operators';
import { selectAuthToken } from '../auth/store/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private store: Store) {}

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
        if (this.isApiRequest(req) && token) {
          req = req.clone({
            headers: req.headers.set('Authorization', 'Bearer ' + token),
          });

          if (workspace) {
            req = req.clone({
              headers: req.headers.set('workspace', workspace),
            });
          }
        }
        return next.handle(req);
      })
    );
  }

  isApiRequest<T>(req: HttpRequest<T>): boolean {
    return req.url.startsWith(environment.apiEndpoint);
  }
}
