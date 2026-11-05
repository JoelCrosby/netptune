import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { RegisterRequest } from '@app/core/models/register-request';
import { ClientResponse } from '../models/client-response';
import { LoginRequest } from '../models/login-request';
import {
  AcceptWorkspaceInviteResponse,
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

  requestPasswordReset(email: string, turnstile: string) {
    return this.http.post<ClientResponse>('api/auth/request-password-reset', {
      email,
      turnstile,
    });
  }

  acceptWorkspaceInvite(code: string) {
    return this.http.post<ClientResponse<AcceptWorkspaceInviteResponse>>(
      'api/auth/accept-workspace-invite',
      { code }
    );
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
