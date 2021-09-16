import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { AuthCodeRequest } from '@core/auth/store/auth.models';

@Injectable()
export class ConfirmEmailResolver implements Resolve<AuthCodeRequest | null> {
  resolve(route: ActivatedRouteSnapshot) {
    const userId = route.queryParamMap.get('userId');
    const code = route.queryParamMap.get('code');

    if (!userId || !code) return null;

    const request: AuthCodeRequest = {
      userId,
      code,
    };

    return request;
  }
}
