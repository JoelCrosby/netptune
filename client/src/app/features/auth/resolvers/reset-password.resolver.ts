import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { clearUserInfo } from '@core/auth/store/auth.actions';
import { AuthCodeRequest } from '@core/auth/store/auth.models';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';

export const resetPassword: ResolveFn<AuthCodeRequest | null> = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store<AppState>);

  store.dispatch(clearUserInfo());

  const userId = route.queryParamMap.get('userId');
  const code = route.queryParamMap.get('code');

  if (!userId || !code) {
    return null;
  }

  const request: AuthCodeRequest = {
    userId,
    code,
  };

  return request;
};
