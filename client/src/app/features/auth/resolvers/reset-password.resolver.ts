import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { clearUserInfo } from '@core/auth/store/auth.actions';
import { AuthCodeRequest } from '@core/auth/store/auth.models';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';

@Injectable()
export class ResetPasswordResolver implements Resolve<AuthCodeRequest | null> {
  constructor(private store: Store<AppState>) {}

  resolve(route: ActivatedRouteSnapshot) {
    this.store.dispatch(clearUserInfo());

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
  }
}
