import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { RegisterRequest } from '@app/core/models/register-request';
import { ClientResponse } from '../models/client-response';
import { LoginRequest } from '../models/login-request';
import {
  AuthCodeRequest,
  LinkProviderRequest,
  LoginResponse,
  ResetPasswordRequest,
  UserResponse,
} from '@core/models/session';

@Service()
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
