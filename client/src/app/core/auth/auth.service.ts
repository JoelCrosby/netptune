import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { RegisterRequest } from '@app/core/models/register-request';
import { ClientResponse } from '../models/client-response';
import {
  AuthCodeRequest,
  LoginResponse,
  ResetPasswordRequest,
  UserResponse,
} from '../store/auth/auth.models';
import { LoginRequest } from '../models/login-request';

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
    return this.http.get<LoginResponse>('api/auth/confirm-email', {
      params: { ...request },
    });
  }

  requestPasswordReset(email: string) {
    return this.http.get<ClientResponse>('api/auth/request-password-reset', {
      params: { email },
    });
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.get<LoginResponse>('api/auth/reset-password', {
      params: { ...request },
    });
  }

  refresh() {
    return this.http.post<LoginResponse>('api/auth/refresh', null);
  }

  logout() {
    return this.http.post<void>('api/auth/logout', null);
  }
}
