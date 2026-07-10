import { HttpClient } from '@angular/common/http';
import {
  EnvironmentProviders,
  Injectable,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { RegisterRequest } from '@app/core/models/register-request';
import { Store } from '@ngrx/store';
import { catchError, firstValueFrom, of, tap } from 'rxjs';
import { ClientResponse } from '../models/client-response';
import { LoginRequest } from '../models/login-request';
import { refreshTokenSuccess } from '../store/auth/auth.actions';
import {
  AuthCodeRequest,
  LinkProviderRequest,
  LoginResponse,
  ResetPasswordRequest,
  UserResponse,
} from '../store/auth/auth.models';

export function provideAuthRefresh(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const authService = inject(AuthService);
    const store = inject(Store);

    if (window.location.pathname === '/auth/auth-provider-login') {
      return firstValueFrom(of(null));
    }

    return firstValueFrom(
      authService.refresh().pipe(
        tap((user) => store.dispatch(refreshTokenSuccess({ user }))),
        catchError(() => of(null))
      )
    );
  });
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  currentUser() {
    return this.http.get<UserResponse>('api/auth/current-user');
  }

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>('api/auth/login', request);
  }

  register(request: RegisterRequest) {
    return this.http.post<LoginResponse>('api/auth/register', request);
  }

  confirmEmail(request: AuthCodeRequest) {
    return this.http.post<LoginResponse>('api/auth/confirm-email', request);
  }

  requestPasswordReset(email: string) {
    return this.http.post<ClientResponse>('api/auth/request-password-reset', {
      email,
    });
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.post<LoginResponse>('api/auth/reset-password', request);
  }

  refresh() {
    return this.http.post<LoginResponse>('api/auth/refresh', null);
  }

  logout() {
    return this.http.post('api/auth/logout', null);
  }

  linkProvider(request: LinkProviderRequest) {
    return this.http.post<LoginResponse>('api/auth/link-provider', request);
  }
}
