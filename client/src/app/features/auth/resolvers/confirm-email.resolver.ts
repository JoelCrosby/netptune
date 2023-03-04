import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { AuthCodeRequest } from '@core/auth/store/auth.models';

export const confirmEmail: ResolveFn<AuthCodeRequest | null> = (
  route: ActivatedRouteSnapshot
) => {
  const userId = route.queryParamMap.get('userId');
  const code = route.queryParamMap.get('code');

  if (!userId || !code) return null;

  const request: AuthCodeRequest = {
    userId,
    code,
  };

  return request;
};
