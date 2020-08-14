import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ConfirmDialogOptions } from '@app/entry/dialogs/confirm-dialog/confirm-dialog.component';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, select, Store } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import { AuthService } from '../auth.service';
import * as actions from './auth.actions';
import { User } from './auth.models';
import { selectAuthState } from './auth.selectors';
import { openSideNav } from '@app/core/store/layout/layout.actions';

export const AUTH_KEY = 'AUTH';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions<Action>,
    private localStorageService: LocalStorageService,
    private router: Router,
    private authService: AuthService,
    private store: Store,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar
  ) {}

  persistSettings = createEffect(
    () =>
      this.actions$.pipe(
        ofType(
          actions.loginFail,
          actions.loginSuccess,
          actions.logout,
          actions.logoutSuccess,
          actions.tryLogin,
          actions.registerSuccess,
          actions.registerFail,
          actions.confirmEmailSuccess,
          actions.confirmEmailFail
        ),
        withLatestFrom(this.store.pipe(select(selectAuthState))),
        tap(([action, settings]) =>
          this.localStorageService.setItem(AUTH_KEY, settings)
        )
      ),
    { dispatch: false }
  );

  tryLogin$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.tryLogin),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.login(action.request).pipe(
            map((userInfo: User) => actions.loginSuccess({ userInfo })),
            tap(() => this.router.navigate(['/workspaces'])),
            tap(() => this.store.dispatch(openSideNav())),
            catchError((error) => of(actions.loginFail({ error })))
          )
        )
      )
  );

  register$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.register),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.register(action.request).pipe(
            map((userInfo: User) => actions.registerSuccess({ userInfo })),
            tap(() => this.router.navigate(['/workspaces'])),
            catchError((error) => of(actions.registerFail({ error })))
          )
        )
      )
  );

  confirmEmail$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.confirmEmail),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.confirmEmail(action.request).pipe(
            map((userInfo: User) => actions.confirmEmailSuccess({ userInfo })),
            tap(() => this.router.navigate(['/workspaces'])),
            tap(() => this.snackbar.open('Email confirmed successfully')),
            catchError((error) => of(actions.confirmEmailFail({ error })))
          )
        )
      )
  );

  confirmEmailFail$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.confirmEmailFail),
        debounceTime(debounce, scheduler),
        tap(() => this.router.navigate(['/auth/login'])),
        tap(() =>
          this.snackbar.open('Email confirmation code is invalid or expired')
        ),
        map(() => actions.logoutSuccess())
      )
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.logout),
      switchMap(() =>
        this.confirmation.open(LOGOUT_CONFIRMATION).pipe(
          map((result) => {
            if (!result) return { type: 'NO_ACTION' };

            this.router.navigate(['auth/login']);
            return actions.logoutSuccess();
          })
        )
      )
    )
  );
}

const LOGOUT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Logout',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to logout?',
  title: 'Logout',
};
