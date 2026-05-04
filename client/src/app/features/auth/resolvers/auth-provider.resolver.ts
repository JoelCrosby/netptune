import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { loginSuccess } from '@app/core/store/auth/auth.actions';
import { LoginResponse } from '@app/core/store/auth/auth.models';
import { Store } from '@ngrx/store';

export const authProvider: ResolveFn<boolean> = async (
  route: ActivatedRouteSnapshot
): Promise<boolean> => {
  const store = inject(Store);

  const expiresValue = route.queryParamMap.get('expires');
  const email = route.queryParamMap.get('email');
  const userId = route.queryParamMap.get('userId');

  if (!expiresValue || !email || !userId) {
    console.error('auth redirect query params failed: ', {
      expiresValue,
      email,
      userId,
    });

    return false;
  }

  const expires = new Date(expiresValue);

  if (Number.isNaN(expires.getTime())) {
    console.error('auth redirect expires date time failed: ', {
      expires,
    });

    return false;
  }

  const displayName = route.queryParamMap.get('displayName') ?? '';
  const pictureUrl = route.queryParamMap.get('pictureUrl') ?? '';

  const user: LoginResponse = {
    userId,
    email,
    expires: expiresValue,
    displayName,
    pictureUrl,
  };

  store.dispatch(loginSuccess({ user }));

  return true;
};
