import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { selectAuthFeature } from '@core/core.state';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { openSideNav } from '@core/store/layout/layout.actions';
import { loadWorkspaces } from '@core/store/workspaces/workspaces.actions';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType, OnInitEffects } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { CookieService } from 'ngx-cookie-service';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  filter,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import { AuthService } from '../auth.service';
import * as actions from './auth.actions';
import { selectIsAuthenticated } from './auth.selectors';

export const AUTH_KEY = 'AUTH';

@Injectable()
export class AuthEffects implements OnInitEffects {
  init$ = createEffect(() =>
    this.actions$.pipe(
      ofType('[Auth]: Init'),
      map(() => actions.currentUser())
    )
  );

  persistSettings = createEffect(
    () =>
      this.actions$.pipe(
        ofType(
          actions.loginSuccess,
          actions.logout,
          actions.logoutSuccess,
          actions.login,
          actions.registerSuccess,
          actions.registerFail,
          actions.confirmEmailSuccess,
          actions.confirmEmailFail,
          actions.currentUserSuccess
        ),
        withLatestFrom(this.store.select(selectAuthFeature)),
        tap(([_, settings]) => {
          const { token, currentUser } = settings;
          this.localStorageService.setItem(AUTH_KEY, { token, currentUser });
        })
      ),
    { dispatch: false }
  );

  fetchCurrentUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        actions.loginSuccess,
        actions.registerSuccess,
        actions.confirmEmailSuccess
      ),
      map(() => actions.currentUser())
    )
  );

  currentUser$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.currentUser),
        debounceTime(debounce, scheduler),
        withLatestFrom(this.store.select(selectIsAuthenticated)),
        filter(([_, auth]) => auth),
        switchMap(() =>
          this.authService.currentUser().pipe(
            map((user) => actions.currentUserSuccess({ user })),
            catchError((error: HttpErrorResponse) =>
              of(actions.currentUserFail({ error }))
            )
          )
        )
      )
  );

  currentUserFail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.currentUserFail),
      map(() => actions.logout({ silent: true }))
    )
  );

  login$ = createEffect(({ debounce = 500, scheduler = asyncScheduler } = {}) =>
    this.actions$.pipe(
      ofType(actions.login),
      debounceTime(debounce, scheduler),
      switchMap((action) =>
        this.authService.login(action.request).pipe(
          map((token) => actions.loginSuccess({ token })),
          catchError(() => of(actions.loginFail()))
        )
      )
    )
  );

  loginSuccess$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loginSuccess),
        debounceTime(debounce, scheduler),
        tap(() => void this.router.navigate(['/workspaces'])),
        switchMap(() => [openSideNav(), loadWorkspaces()])
      )
  );

  register$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.register),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.register(action.request).pipe(
            map((token) => actions.registerSuccess({ token })),
            tap(() => void this.router.navigate(['/workspaces'])),
            catchError((error: HttpErrorResponse) =>
              of(actions.registerFail({ error }))
            )
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
            map((token) => actions.confirmEmailSuccess({ token })),
            tap(() => void this.router.navigate(['/workspaces'])),
            tap(() => this.snackbar.open('Email confirmed successfully')),
            catchError((error: HttpErrorResponse) =>
              of(actions.confirmEmailFail({ error }))
            )
          )
        )
      )
  );

  confirmEmailFail$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.confirmEmailFail),
        debounceTime(debounce, scheduler),
        tap(() => void this.router.navigate(['/auth/login'])),
        tap(() =>
          this.snackbar.open('Email confirmation code is invalid or expired')
        ),
        map(() => actions.logoutSuccess())
      )
  );

  requestPasswordReset$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.requestPasswordReset),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.requestPasswordReset(action.email).pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Password reset email has been sent')),
            tap(() => void this.router.navigate(['/auth/login'])),
            map(() => actions.requestPasswordResetSuccess()),
            catchError((error) =>
              of(actions.requestPasswordResetFail({ error }))
            )
          )
        )
      )
  );

  resetPassword$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.resetPassword),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.resetPassword(action.request).pipe(
            map((token) => actions.resetPasswordSuccess({ token })),
            tap(() => void this.router.navigate(['/workspaces'])),
            tap(() => this.snackbar.open('Password has been reset')),
            catchError((error: HttpErrorResponse) =>
              of(actions.resetPasswordFail({ error }))
            )
          )
        )
      )
  );

  resetPasswordFail$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.resetPasswordFail),
        debounceTime(debounce, scheduler),
        tap(() => void this.router.navigate(['/auth/login'])),
        tap(() =>
          this.snackbar.open('Reset password request is invalid or expired')
        ),
        map(() => actions.logoutSuccess())
      )
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.logout),
      switchMap((action) =>
        this.confirmation.open(LOGOUT_CONFIRMATION, action.silent).pipe(
          map((result) => {
            if (!result) return { type: 'NO_ACTION' };

            this.cookie.deleteAll();
            void this.router.navigate(['/auth/login']);

            return actions.logoutSuccess();
          })
        )
      )
    )
  );

  clearUserInfo$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.clearUserInfo),
        tap(() => this.localStorageService.removeItem(AUTH_KEY))
      ),
    { dispatch: false }
  );

  constructor(
    private actions$: Actions<Action>,
    private localStorageService: LocalStorageService,
    private router: Router,
    private authService: AuthService,
    private store: Store,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar,
    private cookie: CookieService
  ) {}

  ngrxOnInitEffects(): Action {
    return { type: '[Auth]: Init' };
  }
}

const LOGOUT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Logout',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to logout?',
  title: 'Logout',
};
