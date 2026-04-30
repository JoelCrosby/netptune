import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { clearUserInfo } from '@app/core/store/auth/auth.actions';
import { AuthCodeRequest } from '@app/core/store/auth/auth.models';
import { Store } from '@ngrx/store';

export const resetPassword: ResolveFn<AuthCodeRequest | null> = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);

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
