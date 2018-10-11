import { Injectable, Inject } from '@angular/core';
import { Token } from '../../models/token';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {

    public userLoggedIn = false;
    public isLoginCheckReady = false;

    public loginError: string;
    public registerError: string;

    public token: Token = new Token();

    public userName: string;
    public email: string;

    public onLogout = new Subject<void>();

    public httpOptions = {
        headers: new HttpHeaders({
            'Content-Type': 'application/json'
        })
    };

    constructor(private http: HttpClient, private router: Router, @Inject('BASE_URL') private baseUrl: string) { }

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
            this.userName = token.username;
            this.email = token.email;

            return false;
        }

        this.isLoginCheckReady = true;

        return true;
    }

    login(email: string, password: string): void {

        this.loginError = '';

        const body = `{"username": "${email}", "password": "${password}"}`;

        this.http.post<Token>(this.baseUrl + 'api/auth/login', body, this.httpOptions)
            .subscribe((data: Token) => {

                this.token = data;

                localStorage.setItem('auth_token', JSON.stringify(this.token));
                this.userLoggedIn = true;
                this.userName = this.token.username;
                this.email = this.token.email;
                this.router.navigate(['/home']);
            }, error => {
                if (error.error) {
                    this.loginError = error.error;
                } else if (error.error.error_description) {
                    this.loginError = error.error.error_description;
                } else {
                    console.log(error);
                    console.log(error.message);
                    this.loginError = error.message;
                }

                console.log(error);
                this.userLoggedIn = false;
            });
    }

    register(email: string, password: string, username: string): void {
        const body = `{"email": "${email}", "password": "${password}", "username": "${username}"}`;

        this.http.post<Token>(this.baseUrl + 'api/auth/Register', body, this.httpOptions)
            .subscribe(data => {

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
                this.router.navigate(['/home']);
            }, error => {
                if (error.error.Message) {
                    this.registerError = error.error.Message;
                } else if (error.error.error_description) {
                    this.registerError = error.error.error_description;
                } else { this.registerError = JSON.parse(error.error); }

                console.log(error);
            });
    }

    logout() {
        localStorage.removeItem('auth_token');
        this.userLoggedIn = false;
        this.userName = null;
        this.onLogout.next();
        this.router.navigate(['/login']);
    }

}
