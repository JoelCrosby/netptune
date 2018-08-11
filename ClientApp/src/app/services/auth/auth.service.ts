import { Injectable, Inject } from '@angular/core';
import { Token } from '../../models/token';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  public userLoggedIn = false;
  public isLoginCheckReady = false;
  public loginError = '';

  public token: Token = new Token();

  public userName = '';
  public email = '';

  public httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private http: HttpClient, private router: Router, @Inject('BASE_URL') private baseUrl: string) { }

  isTokenExpired(): boolean {

    const tokenString = localStorage.getItem('auth_token');
    console.log('token from local storage isTokenExpired method:' + tokenString);
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
        this.userName = token.username;
        this.email = token.email;

        console.log('token valid:' + tokenString);
        return false;
    }

    console.log('token not valid:' + tokenString);

    this.isLoginCheckReady = true;

    return true;
  }

  login(email: string, password: string): void {

    this.loginError = '';

    const body = `{"username": "${email}", "password": "${password}"}`;

    this.http.post<Token>(this.baseUrl + 'api/auth/login', body, this.httpOptions)
        .subscribe(data => {

            console.log(data);

            this.token.email = data.email;
            this.token.displayName = data.displayName;
            this.token.expires = data.expires;
            this.token.issued = data.issued;
            this.token.token = data.token;
            this.token.expires_in = data.expires_in;
            this.token.token_type = data.token_type;
            this.token.username = data.username;

            localStorage.setItem('auth_token', JSON.stringify(this.token));
            this.userLoggedIn = true;
            this.userName = this.token.username;
            this.email = this.token.email;
            this.changeRoute('');
        }, error => {
            if (error.error.Message) {
                this.loginError = error.error.Message;
            } else if (error.error.error_description) {
                this.loginError = error.error.error_description;
            } else { this.loginError = JSON.parse(error.error); }

            console.log(error);
            this.userLoggedIn = false;
        });
    }

    logout() {
      localStorage.removeItem('auth_token');
      this.userLoggedIn = false;
      this.userName = '';
    }

    changeRoute(route: string) {
      this.router.navigate([route]);
    }
}
