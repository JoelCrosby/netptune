import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { AuthCodeRequest } from '@core/auth/store/auth.models';

@Injectable()
export class ResetPasswordResolver implements Resolve<AuthCodeRequest> {
  resolve(route: ActivatedRouteSnapshot) {
    const userId = route.queryParamMap.get('userId');
    const code = route.queryParamMap.get('code');

    const request: AuthCodeRequest = {
      userId,
      code,
    };

    return request;
  }
}
