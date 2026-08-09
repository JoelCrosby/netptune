import { inject, Injectable, signal } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { hasPendingProviderLink } from '@core/auth/pending-provider-link';
import { LoginRequest } from '@core/models/login-request';
import { RegisterRequest } from '@core/models/register-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import {
  currentUserLoaded,
  logoutSuccess,
  sessionEstablished,
} from '@core/store/auth/auth.actions';
import {
  AuthCodeRequest,
  ResetPasswordRequest,
} from '@core/store/auth/auth.models';
import { selectIsAuthenticated } from '@core/store/auth/auth.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, filter, finalize, switchMap, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthCommandsService {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);

  private readonly loggingIn = signal(false);
  private readonly loginFailed = signal(false);
  private readonly registering = signal(false);
  private readonly confirmingEmail = signal(false);
  private readonly requestingPasswordReset = signal(false);
  private readonly resettingPassword = signal(false);

  readonly loginLoading = this.loggingIn.asReadonly();
  readonly loginError = this.loginFailed.asReadonly();
  readonly registerLoading = this.registering.asReadonly();
  readonly confirmEmailLoading = this.confirmingEmail.asReadonly();
  readonly requestPasswordResetLoading =
    this.requestingPasswordReset.asReadonly();
  readonly resetPasswordLoading = this.resettingPassword.asReadonly();

  login(request: LoginRequest) {
    this.loggingIn.set(true);
    this.loginFailed.set(false);

    this.authService
      .login(request)
      .pipe(
        catchError(() => {
          this.loginFailed.set(true);

          return EMPTY;
        }),
        finalize(() => this.loggingIn.set(false))
      )
      .subscribe((user) => {
        this.store.dispatch(sessionEstablished({ user }));

        // Linking an external provider bounces through login, and the link has to
        // be finished before the workspace it was started from is any use.
        if (hasPendingProviderLink()) {
          void this.router.navigate(['/auth/link-provider']);

          return;
        }

        void this.router.navigate(['/workspaces']);
      });
  }

  clearLoginError() {
    this.loginFailed.set(false);
  }

  register(request: RegisterRequest) {
    this.registering.set(true);

    this.authService
      .register(request)
      .pipe(
        catchError(() => EMPTY),
        finalize(() => this.registering.set(false))
      )
      .subscribe((user) => {
        this.store.dispatch(sessionEstablished({ user }));
        void this.router.navigate(['/workspaces']);
      });
  }

  confirmEmail(request: AuthCodeRequest) {
    this.confirmingEmail.set(true);

    this.authService
      .confirmEmail(request)
      .pipe(
        catchError(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Email confirmation code is invalid or expired`
          );
          void this.router.navigate(['/auth/login']);
          this.store.dispatch(logoutSuccess());

          return EMPTY;
        }),
        finalize(() => this.confirmingEmail.set(false))
      )
      .subscribe((user) => {
        this.store.dispatch(sessionEstablished({ user }));
        void this.router.navigate(['/workspaces']);
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Email confirmed successfully`
        );
      });
  }

  requestPasswordReset(email: string) {
    this.requestingPasswordReset.set(true);

    this.authService
      .requestPasswordReset(email)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.requestingPasswordReset.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Password reset email has been sent`
        );
        void this.router.navigate(['/auth/login']);
      });
  }

  resetPassword(request: ResetPasswordRequest) {
    this.resettingPassword.set(true);

    this.authService
      .resetPassword(request)
      .pipe(
        catchError(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Reset password request is invalid or expired`
          );
          void this.router.navigate(['/auth/login']);
          this.store.dispatch(logoutSuccess());

          return EMPTY;
        }),
        finalize(() => this.resettingPassword.set(false))
      )
      .subscribe((user) => {
        this.store.dispatch(sessionEstablished({ user }));
        void this.router.navigate(['/workspaces']);
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Password has been reset`
        );
      });
  }

  logout(options: { silent?: boolean } = {}) {
    this.confirmation
      .open(LOGOUT_CONFIRMATION, options.silent)
      .pipe(
        filter(Boolean),
        switchMap(() => this.authService.logout()),
        tap(() => void this.router.navigate(['/auth/login'])),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.store.dispatch(logoutSuccess()));
  }

  // The signed-in user carries their permissions, so anything that can change them
  // has to fetch the user again.
  refreshCurrentUser() {
    const isAuthenticated = this.store.selectSignal(selectIsAuthenticated)();
    const workspace = inject(CurrentWorkspaceService).workspace();

    if (!isAuthenticated || !workspace) return;

    this.authService
      .currentUser()
      .pipe(catchError(() => EMPTY))
      .subscribe((user) => this.store.dispatch(currentUserLoaded({ user })));
  }
}

const LOGOUT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Logout`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to logout?`,
  title: $localize`:Title of a confirmation dialog:Logout`,
};
