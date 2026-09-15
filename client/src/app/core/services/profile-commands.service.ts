import { inject, Service, signal } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { SetPasswordRequest } from '@core/models/requests/set-password-request';
import { ProfileService } from '@core/services/profile.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { getErrorMessage } from '@core/util/error-message';
import { gravatarUrl, imageLoads } from '@core/util/gravatar';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, finalize } from 'rxjs';

@Service()
export class ProfileCommandsService {
  private readonly profile = inject(ProfileService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly authCommands = inject(AuthCommandsService);

  private readonly updating = signal(false);
  private readonly updatingPicture = signal(false);
  private readonly changingPassword = signal(false);
  private readonly settingPassword = signal(false);
  private readonly changePasswordFailure = signal<string | undefined>(
    undefined
  );
  private readonly setPasswordFailure = signal<string | undefined>(undefined);

  readonly isUpdating = this.updating.asReadonly();
  readonly isUpdatingPicture = this.updatingPicture.asReadonly();
  readonly isChangingPassword = this.changingPassword.asReadonly();
  readonly isSettingPassword = this.settingPassword.asReadonly();
  readonly changePasswordError = this.changePasswordFailure.asReadonly();
  readonly setPasswordError = this.setPasswordFailure.asReadonly();

  update(profile: Partial<AppUser> & { id: string }) {
    this.updating.set(true);

    this.profile
      .put(profile)
      .pipe(
        unwrapClientResponse(),
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

  uploadPicture(file: File) {
    const data = new FormData();

    data.append('image', file, file.name);

    this.updatingPicture.set(true);

    this.profile
      .uploadProfilePicture(data)
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY),
        finalize(() => this.updatingPicture.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Profile Picture Updated`
        );
        this.onProfileChanged();
      });
  }

  async useGravatar(userId: string, email: string) {
    this.updatingPicture.set(true);

    const url = await gravatarUrl(email);
    const hasGravatar = await imageLoads(url);

    if (!hasGravatar) {
      this.updatingPicture.set(false);
      this.snackbar.open(
        $localize`:Shown when the user's email address has no Gravatar. EMAIL is the address:No Gravatar found for ${email}:EMAIL:`
      );

      return;
    }

    this.setPictureUrl(
      userId,
      url,
      $localize`:Confirmation shown after an action succeeds:Profile Picture Updated`
    );
  }

  removePicture(userId: string) {
    this.updatingPicture.set(true);
    this.setPictureUrl(
      userId,
      '',
      $localize`:Confirmation shown after an action succeeds:Profile Picture Removed`
    );
  }

  changePassword(request: ChangePasswordRequest) {
    this.changingPassword.set(true);
    this.changePasswordFailure.set(undefined);

    this.profile
      .changePassword(request)
      .pipe(
        unwrapClientResponse(),
        catchError((error: unknown) => {
          /* Not the raw message: unwrapClientResponse prefixes it, and getErrorMessage strips that. */
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
        unwrapClientResponse(),
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

  // Only the picture is sent, so unsaved name or email edits are left alone.
  private setPictureUrl(userId: string, pictureUrl: string, message: string) {
    this.profile
      .put({ id: userId, pictureUrl })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY),
        finalize(() => this.updatingPicture.set(false))
      )
      .subscribe(() => {
        this.snackbar.open(message);
        this.onProfileChanged();
      });
  }

  /* The avatar in the shell comes from the auth slice, not the profile resource. */
  private onProfileChanged() {
    this.workspaceRefresh.refresh(['profile']);
    this.authCommands.refreshCurrentUser();
  }
}
