import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { loginSuccess } from '@core/auth/store/auth.actions';
import { LoginResponse } from '@core/auth/store/auth.models';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';

export const authProvider: ResolveFn<boolean> = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);

  const expiresValue = route.queryParamMap.get('expires');
  const email = route.queryParamMap.get('email');
  const userId = route.queryParamMap.get('userId');

  if (!expiresValue || !email || !userId) {
    return of(false);
  }

  const expires = new Date(expiresValue);
  const displayName = route.queryParamMap.get('displayName') ?? '';
  const pictureUrl = route.queryParamMap.get('pictureUrl') ?? '';

  const user: LoginResponse = {
    expires,
    email,
    userId,
    displayName,
    pictureUrl,
  };

  store.dispatch(loginSuccess({ user }));

  return of(true);
};
