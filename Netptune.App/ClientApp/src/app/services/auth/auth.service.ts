import { Injectable } from '@angular/core';
import { HttpHeaders, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { Token } from '../../models/token';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../../models/api-result';


@Injectable({
  providedIn: 'root'
})
export class AuthService {

  userLoggedIn = false;
  isLoginCheckReady = false;

  token: Token = new Token();

  get userName(): string { return this.token.username; }
  get email(): string { return this.token.email; }

  onLogout = new Subject<void>();

  httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private http: HttpClient, private router: Router) { }

  isTokenExpired(): boolean {

    const tokenString = localStorage.getItem('auth_token');
    if (!tokenString) {
      this.isLoginCheckReady = true;
      return true;

    }
    const token = <Token>JSON.parse(tokenString || '');

    if (!token) {
      console.log('failed parsing token:' + tokenString);

      this.isLoginCheckReady = true;
      return true;
    }

    const expdate = new Date(token.expires);
    this.token = token;
    const today = new Date();

    if (expdate > today) {
      this.userLoggedIn = true;
      return false;
    }

    this.isLoginCheckReady = true;

    return true;
  }

  async login(username: string, password: string): Promise<ApiResult> {

    const body = { username, password };

    try {

      const data: Token = await this.http.post<Token>(
        environment.apiEndpoint + 'api/auth/login',
        body,
        this.httpOptions
      ).toPromise();

      this.token = data;

      localStorage.setItem('auth_token', JSON.stringify(this.token));
      this.userLoggedIn = true;

      return ApiResult.Success();

    } catch (error) {

      this.userLoggedIn = false;
      return ApiResult.FromError(error, 'Login Failed');
    }
  }

  async register(username: string, password: string): Promise<ApiResult> {

    const body = {
      username,
      password
    };

    try {

      const result = await this.http.post<Token>(
        environment.apiEndpoint + 'api/auth/Register',
        body,
        this.httpOptions
      ).toPromise();

      this.token = result;
      localStorage.setItem('auth_token', JSON.stringify(this.token));
      this.userLoggedIn = true;

      return ApiResult.Success();

    } catch (error) {
      return ApiResult.FromError(error, 'Registration Failed');
    }
  }

  logout() {
    localStorage.removeItem('auth_token');
    this.userLoggedIn = false;
    this.onLogout.next();
    this.router.navigate(['/auth/login']);
  }

}
