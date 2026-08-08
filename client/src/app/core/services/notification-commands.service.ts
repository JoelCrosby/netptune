import { inject, Injectable } from '@angular/core';
import { ConfirmationService } from '@core/services/confirmation.service';
import { NotificationsService } from '@core/services/notifications.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, switchMap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationCommandsService {
  private readonly notifications = inject(NotificationsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  /** Reading is incidental to what the user was doing, so a failure stays quiet. */
  markAsRead(id: number) {
    this.notifications
      .markAsRead(id)
      .pipe(catchError(() => EMPTY))
      .subscribe(() => this.refresh());
  }

  markAllAsRead() {
    this.notifications
      .markAllAsRead()
      .pipe(catchError(() => EMPTY))
      .subscribe(() => this.refresh());
  }

  markManyAsRead(ids: number[]) {
    this.notifications.markAsReadMany(ids).subscribe(() => {
      this.snackbar.open(
        ids.length === 1
          ? 'Notification marked as read'
          : `${ids.length} notifications marked as read`
      );
      this.refresh();
    });
  }

  delete(ids: number[]) {
    this.confirmation
      .open(buildDeleteConfirmation(ids.length))
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.notifications.deleteNotifications(ids);
        })
      )
      .subscribe(() => {
        this.snackbar.open(
          ids.length === 1
            ? 'Notification deleted'
            : `${ids.length} notifications deleted`
        );
        this.refresh();
      });
  }

  private refresh() {
    this.workspaceRefresh.refresh(['notifications']);
  }
}

const buildDeleteConfirmation = (count: number): ConfirmDialogOptions => ({
  title:
    count === 1 ? 'Delete notification ?' : `Delete ${count} notifications ?`,
  message:
    count === 1
      ? 'This notification will be permanently removed.'
      : `These ${count} notifications will be permanently removed.`,
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
});
