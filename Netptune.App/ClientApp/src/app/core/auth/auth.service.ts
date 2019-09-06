import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { RegisterRequest } from '@app/core/models/register-request';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(loginReq: { email: string; password: string }) {
    return this.http.post(environment.apiEndpoint + 'api/auth/login', loginReq);
  }

  register(registerReq: RegisterRequest) {
    return this.http.post(environment.apiEndpoint + 'api/auth/register', registerReq);
  }
}
