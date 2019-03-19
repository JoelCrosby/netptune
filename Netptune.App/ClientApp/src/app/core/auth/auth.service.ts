import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(loginReq: { username: string; password: string }) {
    return this.http.post(environment.apiEndpoint + 'api/auth/login', loginReq);
  }
}
