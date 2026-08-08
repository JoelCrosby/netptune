import { inject, Injectable } from '@angular/core';
import { WorkspaceRole } from '@core/enums/workspace-role';
import { ConfirmationService } from '@core/services/confirmation.service';
import { UsersService } from '@core/services/users.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { getErrorMessage } from '@core/util/error-message';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, switchMap } from 'rxjs';

/** Writes outlive the dialog or row that starts them, so they are held here. */
@Injectable({ providedIn: 'root' })
export class UserCommandsService {
  private readonly users = inject(UsersService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  invite(emailAddresses: string[]) {
    this.users
      .inviteUsersToWorkspace(emailAddresses)
      .pipe(this.reportFailure(INVITE_FAILED))
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Invite(s) Sent`
        );
        this.workspaceRefresh.refresh(['users']);
      });
  }

  remove(emailAddresses: string[]) {
    this.confirmation
      .open(REMOVE_USERS_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.users
            .removeUsersFromWorkspace(emailAddresses)
            .pipe(this.reportFailure(REMOVE_FAILED));
        })
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:User(s) removed`
        );
        this.workspaceRefresh.refresh(['users']);
      });
  }

  resendInvite(email: string) {
    this.users
      .resendInvite(email)
      .pipe(this.reportFailure(RESEND_FAILED))
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Invite resent`
        );
      });
  }

  togglePermission(userId: string, permission: string) {
    this.users
      .toggleUserPermission(userId, permission)
      .pipe(this.reportFailure(PERMISSION_FAILED))
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Permission updated`
        );
        this.workspaceRefresh.refresh(['users']);
      });
  }

  updateRole(userId: string, role: WorkspaceRole) {
    this.users
      .updateWorkspaceRole(userId, role)
      .pipe(unwrapClientReposne(), this.reportFailure(ROLE_FAILED))
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Workspace role updated`
        );
        this.workspaceRefresh.refresh(['users']);
      });
  }

  private reportFailure<T>(fallback: string) {
    return catchError<T, typeof EMPTY>((error: unknown) => {
      this.snackbar.error(getErrorMessage(error, fallback));

      return EMPTY;
    });
  }
}

const INVITE_FAILED = $localize`:Error shown after an action fails:The invite(s) could not be sent. Please try again.`;
const PERMISSION_FAILED = $localize`:Error shown after an action fails:The permission could not be updated. Please try again.`;
const ROLE_FAILED = $localize`:Error shown after an action fails:The workspace role could not be updated. Please try again.`;
const RESEND_FAILED = $localize`:Error shown after an action fails:The invite could not be resent. Please try again.`;
const REMOVE_FAILED = $localize`:Error shown after an action fails:The user(s) could not be removed. Please try again.`;

const REMOVE_USERS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Remove User(s)`,
  color: 'warn',
  title: $localize`:Title of a confirmation dialog:Remove users from workspace`,
  message:
    'This will remove the user(s) from the workspace, but will not remove thier accounts.',
};
