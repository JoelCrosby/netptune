import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, switchMap } from 'rxjs/operators';
import { selectAuthToken } from '../auth/store/auth.selectors';
import { AppState } from '../core.state';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private store: Store<AppState>) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return this.store.select(selectAuthToken).pipe(
      first(),
      switchMap((token: string) => {
        if (this.isApiRequest(req) && token) {
          req = req.clone({
            headers: req.headers.set('Authorization', 'Bearer ' + token),
          });
        }
        return next.handle(req);
      })
    );
  }

  isApiRequest(req: HttpRequest<any>): boolean {
    return req.url.startsWith(environment.apiEndpoint);
  }
}
