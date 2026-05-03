import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { selectCurrentWorkspace } from '@app/core/store/workspaces/workspaces.selectors';
import { ConfirmationService } from '@core/services/confirmation.service';
import { openSideNav } from '@core/store/layout/layout.actions';
import { loadWorkspaces } from '@core/store/workspaces/workspaces.actions';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType, OnInitEffects } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  filter,
  map,
  switchMap,
  tap,
} from 'rxjs/operators';
import { AuthService } from '../../auth/auth.service';
import * as actions from './auth.actions';
import { selectIsAuthenticated } from './auth.selectors';

export const AUTH_KEY = 'AUTH';

@Injectable()
export class AuthEffects implements OnInitEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private router = inject(Router);
  private authService = inject(AuthService);
  private store = inject(Store);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);

  init$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initAuth),
      map(() => actions.currentUser())
    );
  });

  currentUser$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.currentUser),
        debounceTime(debounce, scheduler),
        concatLatestFrom(() => [
          this.store.select(selectIsAuthenticated),
          this.store.select(selectCurrentWorkspace),
        ]),
        filter(([_, auth, worksapce]) => !!auth && !!worksapce),
        switchMap(() =>
          this.authService.currentUser().pipe(
            map((user) => actions.currentUserSuccess({ user })),
            catchError((error: HttpErrorResponse) =>
              of(actions.currentUserFail({ error }))
            )
          )
        )
      );
    }
  );

  login$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.login),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.login(action.request).pipe(
            map((user) => actions.loginSuccess({ user })),
            catchError(() => of(actions.loginFail()))
          )
        )
      );
    }
  );

  openSideNav$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loginSuccess),
        debounceTime(debounce, scheduler),
        map(() => openSideNav())
      );
    }
  );

  loadWorkspaces$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loginSuccess),
        debounceTime(debounce, scheduler),
        switchMap(() => this.router.navigate(['/workspaces'])),
        map(() => loadWorkspaces())
      );
    }
  );

  loadWorkspacesAfterRefresh$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.refreshTokenSuccess),
        debounceTime(debounce, scheduler),
        map(() => loadWorkspaces())
      );
    }
  );

  register$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.register),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.register(action.request).pipe(
            map((user) => actions.registerSuccess({ user })),
            tap(() => void this.router.navigate(['/workspaces'])),
            catchError((error: HttpErrorResponse) =>
              of(actions.registerFail({ error }))
            )
          )
        )
      );
    }
  );

  registerSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.registerSuccess),
      map(() => loadWorkspaces())
    );
  });

  confirmEmail$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.confirmEmail),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.confirmEmail(action.request).pipe(
            map((user) => actions.confirmEmailSuccess({ user })),
            tap(() => void this.router.navigate(['/workspaces'])),
            tap(() => this.snackbar.open('Email confirmed successfully')),
            catchError((error: HttpErrorResponse) =>
              of(actions.confirmEmailFail({ error }))
            )
          )
        )
      );
    }
  );

  confirmEmailFail$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.confirmEmailFail),
        debounceTime(debounce, scheduler),
        tap(() => void this.router.navigate(['/auth/login'])),
        tap(() =>
          this.snackbar.open('Email confirmation code is invalid or expired')
        ),
        map(() => actions.logoutSuccess())
      );
    }
  );

  requestPasswordReset$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
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
      );
    }
  );

  resetPassword$ = createEffect(
    ({ debounce = 500, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.resetPassword),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.authService.resetPassword(action.request).pipe(
            map((user) => actions.resetPasswordSuccess({ user })),
            tap(() => void this.router.navigate(['/workspaces'])),
            tap(() => this.snackbar.open('Password has been reset')),
            catchError((error: HttpErrorResponse) =>
              of(actions.resetPasswordFail({ error }))
            )
          )
        )
      );
    }
  );

  resetPasswordFail$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.resetPasswordFail),
        debounceTime(debounce, scheduler),
        tap(() => void this.router.navigate(['/auth/login'])),
        tap(() =>
          this.snackbar.open('Reset password request is invalid or expired')
        ),
        map(() => actions.logoutSuccess())
      );
    }
  );

  logout$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.logout),
      switchMap((action) =>
        this.confirmation.open(LOGOUT_CONFIRMATION, action.silent).pipe(
          filter(Boolean),
          switchMap(() => this.authService.logout()),
          tap(() => void this.router.navigate(['/auth/login'])),
          map(() => actions.logoutSuccess())
        )
      )
    );
  });

  ngrxOnInitEffects(): Action {
    return actions.initAuth();
  }
}

const LOGOUT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Logout',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to logout?',
  title: 'Logout',
};
