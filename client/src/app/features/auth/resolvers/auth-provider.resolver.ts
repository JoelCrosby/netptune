import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { loginSuccess } from '@core/auth/store/auth.actions';
import { UserToken } from '@core/auth/store/auth.models';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';

export const authProvider: ResolveFn<boolean> = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store<AppState>);

  route.queryParamMap.get('code');

  const expiresValue = route.queryParamMap.get('expires');

  if (expiresValue === null) {
    return of(false);
  }

  const expires = new Date(expiresValue);

  const issued = route.queryParamMap.get('issued');
  const token = route.queryParamMap.get('token');
  const email = route.queryParamMap.get('email');
  const userId = route.queryParamMap.get('userId');

  const displayName = route.queryParamMap.get('displayName') ?? '';
  const pictureUrl = route.queryParamMap.get('pictureUrl') ?? '';

  if (!token || !email || !userId) {
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

  store.dispatch(loginSuccess({ token: userToken }));

  return of(true);
};
