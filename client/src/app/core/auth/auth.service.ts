import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { RegisterRequest } from '@core/models/register-request';
import { environment } from '@env/environment';
import { ClientResponse } from '../models/client-response';
import {
  AuthCodeRequest,
  LoginRequest,
  ResetPasswordRequest,
  UserToken,
  UserResponse,
} from './store/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);


  currentUser() {
    return this.http.get<UserResponse>(
      environment.apiEndpoint + 'api/auth/current-user'
    );
  }

  login(request: LoginRequest) {
    return this.http.post<UserToken>(
      environment.apiEndpoint + 'api/auth/login',
      request
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<UserToken>(
      environment.apiEndpoint + 'api/auth/register',
      request
    );
  }

  confirmEmail(request: AuthCodeRequest) {
    return this.http.get<UserToken>(
      environment.apiEndpoint + 'api/auth/confirm-email',
      {
        params: { ...request },
      }
    );
  }

  requestPasswordReset(email: string) {
    return this.http.get<ClientResponse>(
      environment.apiEndpoint + 'api/auth/request-password-reset',
      {
        params: { email },
      }
    );
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.get<UserToken>(
      environment.apiEndpoint + 'api/auth/reset-password',
      {
        params: { ...request },
      }
    );
  }
}
