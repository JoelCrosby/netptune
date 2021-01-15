import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { loginSuccess } from '@core/auth/store/auth.actions';
import { UserToken } from '@core/auth/store/auth.models';
import { Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';

@Injectable()
export class AuthProviderResolver implements Resolve<Observable<boolean>> {
  constructor(private store: Store) {}

  resolve(route: ActivatedRouteSnapshot) {
    route.queryParamMap.get('code');

    const token: UserToken = {
      expires: new Date(route.queryParamMap.get('expires')),
      issued: route.queryParamMap.get('issued'),
      token: route.queryParamMap.get('token'),
      email: route.queryParamMap.get('emailAddress'),
      userId: route.queryParamMap.get('userId'),
      displayName: route.queryParamMap.get('displayName'),
      pictureUrl: route.queryParamMap.get('pictureUrl'),
    };

    this.store.dispatch(loginSuccess({ token }));

    return of(true);
  }
}
