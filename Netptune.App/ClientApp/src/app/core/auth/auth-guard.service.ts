import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { map } from 'rxjs/operators';
import { AuthState } from './store/auth.models';
import { selectIsAuthenticated } from './store/auth.selectors';

@Injectable({ providedIn: 'root' })
export class AuthGuardService implements CanActivate {
  constructor(private store: Store<AuthState>, private router: Router) {}

  canActivate() {
    return this.store.pipe(
      select(selectIsAuthenticated),
      map(result => this.handleAuthenticationState(result))
    );
  }

  handleAuthenticationState(state: boolean): boolean {
    if (!state) {
      this.router.navigate(['/auth/login']);
    }
    return state;
  }
}
