import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { RegisterRequest } from '@core/models/register-request';
import { LoginRequest } from './store/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(loginReq: LoginRequest) {
    return this.http.post(environment.apiEndpoint + 'api/auth/login', loginReq);
  }

  register(registerReq: RegisterRequest) {
    return this.http.post(
      environment.apiEndpoint + 'api/auth/register',
      registerReq
    );
  }
}
