import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { LocalStorageService } from '@app/core/local-storage/local-storage.service';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap } from 'rxjs/operators';
import { AuthService } from '../auth.service';
import {
  ActionAuthLoginFail,
  ActionAuthLoginSuccess,
  ActionAuthTryLogin,
  AuthActionTypes,
  ActionAuthLogout,
} from './auth.actions';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions<Action>,
    private localStorageService: LocalStorageService,
    private router: Router,
    private authService: AuthService
  ) {}

  @Effect({ dispatch: false })
  logOut$ = this.actions$.pipe(
    ofType<ActionAuthLogout>(AuthActionTypes.LOGOUT),
    tap(() => this.router.navigate(['auth/login']))
  );

  @Effect()
  tryLogin$ = ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
    this.actions$.pipe(
      ofType<ActionAuthTryLogin>(AuthActionTypes.TRY_LOGIN),
      debounceTime(debounce, scheduler),
      switchMap((action: ActionAuthTryLogin) =>
        this.authService.login(action.payload).pipe(
          map((userInfo: any) => new ActionAuthLoginSuccess(userInfo)),
          tap(() => this.router.navigate(['/projects'])),
          catchError(error => of(new ActionAuthLoginFail({ error })))
        )
      )
    );
}
