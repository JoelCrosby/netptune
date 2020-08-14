import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { tap, map } from 'rxjs/operators';
import { AuthState } from './store/auth.models';
import { selectIsAuthenticated } from './store/auth.selectors';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoginGuardService implements CanActivate {
  constructor(private store: Store<AuthState>, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.store.pipe(
      select(selectIsAuthenticated),
      map((result) => !result)
    );
  }
}
