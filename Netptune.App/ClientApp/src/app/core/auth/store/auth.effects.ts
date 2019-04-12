import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { LocalStorageService } from '@app/core/local-storage/local-storage.service';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Action, Store, select } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap, withLatestFrom } from 'rxjs/operators';
import { AuthService } from '../auth.service';
import {
  ActionAuthLoginFail,
  ActionAuthLoginSuccess,
  ActionAuthTryLogin,
  AuthActionTypes,
  ActionAuthLogout,
  ActionAuthRegister,
  ActionAuthRegisterSuccess,
  ActionAuthRegisterFail,
} from './auth.actions';
import { AppState } from '../../core.state';
import { selectAuthState } from './auth.selectors';
import { ActionSettingsClear } from '../../../features/settings/store/settings.actions';

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
    ofType<ActionAuthLogout>(AuthActionTypes.LOGOUT),
    tap(() => this.router.navigate(['auth/login']))
  );

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(
      AuthActionTypes.LOGIN_FAIL,
      AuthActionTypes.LOGIN_SUCCESS,
      AuthActionTypes.LOGOUT,
      AuthActionTypes.TRY_LOGIN
    ),
    withLatestFrom(this.store.pipe(select(selectAuthState))),
    tap(([action, settings]) => this.localStorageService.setItem(AUTH_KEY, settings))
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
    )

  @Effect()
  register$ = ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
    this.actions$.pipe(
      ofType<ActionAuthRegister>(AuthActionTypes.REGISTER),
      debounceTime(debounce, scheduler),
      switchMap((action: ActionAuthRegister) =>
        this.authService.register(action.payload).pipe(
          map((userInfo: any) => new ActionAuthRegisterSuccess(userInfo)),
          tap(() => this.router.navigate(['/projects'])),
          catchError(error => of(new ActionAuthRegisterFail({ error })))
        )
      )
    )

  @Effect()
  logout$ = () =>
    this.actions$.pipe(
      ofType<ActionAuthTryLogin>(AuthActionTypes.LOGOUT),
      map(() => new ActionSettingsClear())
    )
}
