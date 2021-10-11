import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { tap } from 'rxjs/operators';
import { AppState } from './../core.state';
import { selectIsAuthenticated } from './store/auth.selectors';

@Injectable({ providedIn: 'root' })
export class AuthGuardService implements CanActivate {
  constructor(private store: Store<AppState>, private router: Router) {}

  canActivate() {
    return this.store.pipe(
      select(selectIsAuthenticated),
      tap((result) => this.handleAuthenticationState(result))
    );
  }

  handleAuthenticationState(state: boolean) {
    if (!state) {
      void this.router.navigate(['/auth/login']);
    }
  }
}
