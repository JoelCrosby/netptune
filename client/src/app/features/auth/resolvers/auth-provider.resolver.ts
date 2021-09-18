import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { loginSuccess } from '@core/auth/store/auth.actions';
import { UserToken } from '@core/auth/store/auth.models';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';

@Injectable()
export class AuthProviderResolver implements Resolve<Observable<boolean>> {
  constructor(private store: Store<AppState>) {}

  resolve(route: ActivatedRouteSnapshot) {
    route.queryParamMap.get('code');

    const expiresValue = route.queryParamMap.get('expires');

    if (expiresValue === null) {
      console.log('activated route auth code faild');
      return of(false);
    }

    const expires = new Date(expiresValue);

    const issued = route.queryParamMap.get('issued');
    const token = route.queryParamMap.get('token');
    const email = route.queryParamMap.get('emailAddress');
    const userId = route.queryParamMap.get('userId');

    const displayName = route.queryParamMap.get('displayName') ?? '';
    const pictureUrl = route.queryParamMap.get('pictureUrl') ?? '';

    if (!token || !email || !userId) {
      console.log('activated route auth code faild');
      return of(false);
    }

    const userToken: UserToken = {
      expires,
      issued,
      token,
      email,
      userId,
      displayName,
      pictureUrl,
    };

    this.store.dispatch(loginSuccess({ token: userToken }));

    return of(true);
  }
}
