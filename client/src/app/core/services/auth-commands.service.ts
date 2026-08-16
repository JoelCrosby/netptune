import { inject, Service, signal } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { hasPendingProviderLink } from '@core/auth/pending-provider-link';
import { LoginRequest } from '@core/models/login-request';
import { RegisterRequest } from '@core/models/register-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import { AuthCodeRequest, ResetPasswordRequest } from '@core/models/session';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, filter, finalize, switchMap, tap } from 'rxjs';

@Service()
export class AuthCommandsService {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly workspaceList = inject(WorkspaceListService);
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
        this.session.establish(user);

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
        this.session.establish(user);
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
          this.endSession();

          return EMPTY;
        }),
        finalize(() => this.confirmingEmail.set(false))
      )
      .subscribe((user) => {
        this.session.establish(user);
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
        unwrapClientResponse(),
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
          this.endSession();

          return EMPTY;
        }),
        finalize(() => this.resettingPassword.set(false))
      )
      .subscribe((user) => {
        this.session.establish(user);
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
      .subscribe(() => this.endSession());
  }

  /** Drops everything the signed-in user could see, not just the session itself. */
  endSession() {
    this.session.clear();
    this.currentWorkspace.set(undefined);
    this.workspaceList.clear();
  }

  // The signed-in user carries their permissions, so anything that can change them
  // has to fetch the user again.
  refreshCurrentUser() {
    const isAuthenticated = inject(SessionService).isAuthenticated();
    const workspace = inject(CurrentWorkspaceService).workspace();

    if (!isAuthenticated || !workspace) return;

    this.authService
      .currentUser()
      .pipe(catchError(() => EMPTY))
      .subscribe((user) => this.session.setUser(user));
  }
}

const LOGOUT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Logout`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to logout?`,
  title: $localize`:Title of a confirmation dialog:Logout`,
};
