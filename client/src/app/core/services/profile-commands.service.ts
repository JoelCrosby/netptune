import { inject, Injectable, signal } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { SetPasswordRequest } from '@core/models/requests/set-password-request';
import { ProfileService } from '@core/services/profile.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { getErrorMessage } from '@core/util/error-message';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, finalize } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ProfileCommandsService {
  private readonly profile = inject(ProfileService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly authCommands = inject(AuthCommandsService);

  private readonly updating = signal(false);
  private readonly changingPassword = signal(false);
  private readonly settingPassword = signal(false);
  private readonly changePasswordFailure = signal<string | undefined>(
    undefined
  );
  private readonly setPasswordFailure = signal<string | undefined>(undefined);

  readonly isUpdating = this.updating.asReadonly();
  readonly isChangingPassword = this.changingPassword.asReadonly();
  readonly isSettingPassword = this.settingPassword.asReadonly();
  readonly changePasswordError = this.changePasswordFailure.asReadonly();
  readonly setPasswordError = this.setPasswordFailure.asReadonly();

  update(profile: Partial<AppUser> & { id: string }) {
    this.updating.set(true);

    this.profile
      .put(profile)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.updating.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Profile Updated`
        );
        this.onProfileChanged();
      });
  }

  uploadPicture(data: FormData) {
    this.profile
      .uploadProfilePicture(data)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.onProfileChanged());
  }

  changePassword(request: ChangePasswordRequest) {
    this.changingPassword.set(true);
    this.changePasswordFailure.set(undefined);

    this.profile
      .changePassword(request)
      .pipe(
        unwrapClientReposne(),
        catchError((error: unknown) => {
          /* Not the raw message: unwrapClientReposne prefixes it, and getErrorMessage strips that. */
          this.changePasswordFailure.set(getErrorMessage(error));

          return EMPTY;
        }),
        finalize(() => this.changingPassword.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Password Changed`
        );
      });
  }

  setPassword(request: SetPasswordRequest) {
    this.settingPassword.set(true);
    this.setPasswordFailure.set(undefined);

    this.profile
      .setPassword(request)
      .pipe(
        unwrapClientReposne(),
        catchError((error: unknown) => {
          this.setPasswordFailure.set(getErrorMessage(error));

          return EMPTY;
        }),
        finalize(() => this.settingPassword.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Password Set`
        );
        this.workspaceRefresh.refresh(['profile']);
      });
  }

  /* The avatar in the shell comes from the auth slice, not the profile resource. */
  private onProfileChanged() {
    this.workspaceRefresh.refresh(['profile']);
    this.authCommands.refreshCurrentUser();
  }
}
