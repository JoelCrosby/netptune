import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { UserService } from '../user/user.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuardService {

  onNotAuthenticated = new Subject<void>();

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private router: Router) { }

  async canActivate(): Promise<boolean> {
    if (this.authService.isTokenExpired()) {

      this.onNotAuthenticated.next();

      this.router.navigate(['auth/login']);
      return false;
    }

    if (!this.userService.currentUser) {

      try {
        const user = await this.userService.getUser(this.authService.token.userId).toPromise();
        this.userService.currentUser = user;
      } catch {
        this.router.navigate(['auth/login']);
        return false;
      }
    }

    return true;
  }

}
