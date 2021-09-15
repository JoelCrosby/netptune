import { Injectable } from '@angular/core';
import { CanActivate } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AppState } from './../core.state';
import { selectIsAuthenticated } from './store/auth.selectors';

@Injectable({ providedIn: 'root' })
export class LoginGuardService implements CanActivate {
  constructor(private store: Store<AppState>) {}

  canActivate(): Observable<boolean> {
    return this.store.pipe(
      select(selectIsAuthenticated),
      map((result) => !result)
    );
  }
}
