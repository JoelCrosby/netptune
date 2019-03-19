import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { map } from 'rxjs/operators';
import { AuthState } from './store/auth.reducer';
import { selectIsAuthenticated } from './store/auth.selectors';

@Injectable()
export class AuthGuardService implements CanActivate {
  constructor(private store: Store<AuthState>, private router: Router) {}

  canActivate() {
    return this.store.pipe(
      select(selectIsAuthenticated),
      map(result => {
        if (!result) {
          this.router.navigate(['/auth/login']);
        }
        return result;
      })
    );
  }
}
