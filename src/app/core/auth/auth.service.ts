import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { RegisterRequest } from '@core/models/register-request';
import { LoginRequest, ConfirmEmailRequest, User } from './store/auth.models';

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

  confirmEmail(request: ConfirmEmailRequest) {
    return this.http.get(environment.apiEndpoint + 'api/auth/confirm-email', {
      params: { ...request },
    });
  }
}
