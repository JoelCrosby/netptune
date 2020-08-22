import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RegisterRequest } from '@core/models/register-request';
import { environment } from '@env/environment';
import { ClientResponse } from '../models/client-response';
import {
  AuthCodeRequest,
  LoginRequest,
  ResetPasswordRequest,
  User,
} from './store/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(request: LoginRequest) {
    return this.http.post<User>(
      environment.apiEndpoint + 'api/auth/login',
      request
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<User>(
      environment.apiEndpoint + 'api/auth/register',
      request
    );
  }

  confirmEmail(request: AuthCodeRequest) {
    return this.http.get<User>(
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
    return this.http.get<User>(
      environment.apiEndpoint + 'api/auth/reset-password',
      {
        params: { ...request },
      }
    );
  }
}
