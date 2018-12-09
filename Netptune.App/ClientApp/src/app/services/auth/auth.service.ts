import { Injectable } from '@angular/core';
import { Token } from '../../models/token';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RegisterResult } from '../../models/register-result.ts';

@Injectable({
    providedIn: 'root'
})
export class AuthService {

    public userLoggedIn = false;
    public isLoginCheckReady = false;

    public loginError: string;
    public registerError: string;

    public token: Token = new Token();

    public get userName(): string { return this.token.username; }
    public get email(): string { return this.token.email; }

    public onLogout = new Subject<void>();

    public httpOptions = {
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

    async login(email: string, password: string): Promise<boolean> {

        this.loginError = '';

        const body = `{"username": "${email}", "password": "${password}"}`;

        try {
            const data: Token = await this.http.post<Token>(environment.apiEndpoint + 'api/auth/login', body, this.httpOptions).toPromise();

            this.token = data;

            localStorage.setItem('auth_token', JSON.stringify(this.token));
            this.userLoggedIn = true;
            this.router.navigate(['/home']);

            return true;
        } catch (error) {
            console.error(error);
            this.userLoggedIn = false;

            return false;
        }
    }

    async register(email: string, password: string, username: string): Promise<RegisterResult> {

        const body = {
            email,
            password,
            username
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

            return RegisterResult.Success();

        } catch (error) {
            return RegisterResult.Error(error);
        }
    }

    logout() {
        localStorage.removeItem('auth_token');
        this.userLoggedIn = false;
        this.onLogout.next();
        this.router.navigate(['/login']);
    }

}
