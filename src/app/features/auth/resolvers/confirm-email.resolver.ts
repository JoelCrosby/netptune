import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { ConfirmEmailRequest } from '@core/auth/store/auth.models';

@Injectable()
export class ConfirmEmailResolver implements Resolve<ConfirmEmailRequest> {
  resolve(route: ActivatedRouteSnapshot) {
    const userId = route.queryParamMap.get('userId');
    const code = route.queryParamMap.get('code');

    const request: ConfirmEmailRequest = {
      userId,
      code,
    };

    return request;
  }
}
