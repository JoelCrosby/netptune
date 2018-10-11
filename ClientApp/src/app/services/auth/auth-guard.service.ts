import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuardService {

  public onNotAuthenticated = new Subject<void>();

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(): boolean {
    if (this.authService.isTokenExpired()) {

      this.onNotAuthenticated.next();

      this.router.navigate(['login']);
      return false;
    }
    return true;
  }

}
