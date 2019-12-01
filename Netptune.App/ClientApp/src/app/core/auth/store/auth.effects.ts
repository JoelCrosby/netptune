import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Action, Store, select } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap, withLatestFrom } from 'rxjs/operators';
import { AuthService } from '../auth.service';
import * as AuthActions from './auth.actions';
import { AppState } from '@core/core.state';
import { selectAuthState } from './auth.selectors';
import { ActionSettingsClear } from '@app/core/settings/settings.actions';

export const AUTH_KEY = 'AUTH';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions<Action>,
    private localStorageService: LocalStorageService,
    private router: Router,
    private authService: AuthService,
    private store: Store<AppState>
  ) {}

  @Effect({ dispatch: false })
  logOut$ = this.actions$.pipe(
    ofType<AuthActions.ActionAuthLogout>(AuthActions.AuthActionTypes.LOGOUT),
    tap(() => this.router.navigate(['auth/login']))
  );

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(
      AuthActions.AuthActionTypes.LOGIN_FAIL,
      AuthActions.AuthActionTypes.LOGIN_SUCCESS,
      AuthActions.AuthActionTypes.LOGOUT,
      AuthActions.AuthActionTypes.TRY_LOGIN
    ),
    withLatestFrom(this.store.pipe(select(selectAuthState))),
    tap(([action, settings]) => this.localStorageService.setItem(AUTH_KEY, settings))
  );

  @Effect()
  tryLogin$ = ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
    this.actions$.pipe(
      ofType<AuthActions.ActionAuthTryLogin>(AuthActions.AuthActionTypes.TRY_LOGIN),
      debounceTime(debounce, scheduler),
      switchMap((action: AuthActions.ActionAuthTryLogin) =>
        this.authService.login(action.payload).pipe(
          map((userInfo: any) => new AuthActions.ActionAuthLoginSuccess(userInfo)),
          tap(() => this.router.navigate(['/workspaces'])),
          catchError(error => of(new AuthActions.ActionAuthLoginFail({ error })))
        )
      )
    );

  @Effect()
  register$ = ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
    this.actions$.pipe(
      ofType<AuthActions.ActionAuthRegister>(AuthActions.AuthActionTypes.REGISTER),
      debounceTime(debounce, scheduler),
      switchMap((action: AuthActions.ActionAuthRegister) =>
        this.authService.register(action.payload.request).pipe(
          map((userInfo: any) => new AuthActions.ActionAuthRegisterSuccess(userInfo)),
          tap(() => this.router.navigate(['/workspaces'])),
          catchError(error => of(new AuthActions.ActionAuthRegisterFail({ error })))
        )
      )
    );

  @Effect()
  logout$ = () =>
    this.actions$.pipe(
      ofType<AuthActions.ActionAuthTryLogin>(AuthActions.AuthActionTypes.LOGOUT),
      map(() => new ActionSettingsClear())
    );
}
