import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import * as actions from './notifications.actions';
import { NotificationsService } from './notifications.service';
import { HttpErrorResponse } from '@angular/common/http';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { NotificationSseService } from '@core/sse/notification-sse.service';
import { setCurrentWorkspace } from '@core/store/workspaces/workspaces.actions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';

const buildDeleteConfirmation = (count: number): ConfirmDialogOptions => ({
  title:
    count === 1 ? 'Delete notification ?' : `Delete ${count} notifications ?`,
  message:
    count === 1
      ? 'This notification will be permanently removed.'
      : `These ${count} notifications will be permanently removed.`,
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  color: 'warn',
});

@Injectable()
export class NotificationsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private notificationsService = inject(NotificationsService);
  private notificationSse = inject(NotificationSseService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);

  loadNotifications$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadNotifications.init, actions.notificationReceived),
      switchMap(() =>
        this.notificationsService.getRecent().pipe(
          unwrapClientReposne(),
          map((page) =>
            actions.loadNotifications.success({ notifications: page.items })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadNotifications.fail({ error }))
          )
        )
      )
    );
  });

  loadUnreadCount$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUnreadCount.init),
      switchMap(() =>
        this.notificationsService.getUnreadCount().pipe(
          unwrapClientReposne(),
          map((count) => actions.loadUnreadCount.success({ count })),
          catchError(() => of(actions.loadUnreadCount.success({ count: 0 })))
        )
      )
    );
  });

  markAsRead$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.markAsRead.init),
      switchMap(({ id }) =>
        this.notificationsService.markAsRead(id).pipe(
          map(() => actions.markAsRead.success({ id })),
          catchError(() => of(actions.markAsRead.success({ id })))
        )
      )
    );
  });

  markAllAsRead$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.markAllAsRead.init),
      switchMap(() =>
        this.notificationsService.markAllAsRead().pipe(
          map(() => actions.markAllAsRead.success()),
          catchError(() => of(actions.markAllAsRead.success()))
        )
      )
    );
  });

  markAsReadMany$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.markAsReadMany.init),
      switchMap(({ ids }) =>
        this.notificationsService.markAsReadMany(ids).pipe(
          tap(() =>
            this.snackbar.open(
              ids.length === 1
                ? 'Notification marked as read'
                : `${ids.length} notifications marked as read`
            )
          ),
          map(() => actions.markAsReadMany.success({ ids })),
          catchError((error: HttpErrorResponse) =>
            of(actions.markAsReadMany.fail({ error }))
          )
        )
      )
    );
  });

  deleteNotifications$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteNotifications.init),
      switchMap(({ ids }) =>
        this.confirmation.open(buildDeleteConfirmation(ids.length)).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.notificationsService.deleteNotifications(ids).pipe(
              tap(() =>
                this.snackbar.open(
                  ids.length === 1
                    ? 'Notification deleted'
                    : `${ids.length} notifications deleted`
                )
              ),
              map(() => actions.deleteNotifications.success({ ids })),
              catchError((error: HttpErrorResponse) =>
                of(actions.deleteNotifications.fail({ error }))
              )
            );
          })
        )
      )
    );
  });

  refreshAfterMutation$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(
        actions.markAsReadMany.success,
        actions.deleteNotifications.success
      ),
      switchMap(() => [
        actions.loadUnreadCount.init(),
        actions.loadNotifications.init(),
      ])
    );
  });

  connectSse$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(setCurrentWorkspace),
        tap(() => this.notificationSse.connect())
      );
    },
    { dispatch: false }
  );

  loadUnreadOnConnect$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(setCurrentWorkspace),
      switchMap(() =>
        this.notificationsService.getUnreadCount().pipe(
          unwrapClientReposne(),
          map((count) => actions.loadUnreadCount.success({ count })),
          catchError(() => of(actions.loadUnreadCount.success({ count: 0 })))
        )
      )
    );
  });
}
