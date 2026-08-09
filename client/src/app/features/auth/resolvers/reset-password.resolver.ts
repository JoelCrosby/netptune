import { inject } from '@angular/core';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { AuthCodeRequest } from '@core/models/session';

export const resetPassword: ResolveFn<AuthCodeRequest | null> = (
  route: ActivatedRouteSnapshot
) => {
  inject(AuthCommandsService).endSession();

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
