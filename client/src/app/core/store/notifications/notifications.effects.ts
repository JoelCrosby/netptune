import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import * as actions from './notifications.actions';
import { NotificationsService } from './notifications.service';
import { HttpErrorResponse } from '@angular/common/http';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { NotificationSseService } from '@core/sse/notification-sse.service';
import { setCurrentWorkspace } from '@core/store/workspaces/workspaces.actions';

@Injectable()
export class NotificationsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private notificationsService = inject(NotificationsService);
  private notificationSse = inject(NotificationSseService);

  loadNotifications$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadNotifications, actions.notificationReceived),
      switchMap(() =>
        this.notificationsService.getAll().pipe(
          unwrapClientReposne(),
          map((notifications) => actions.loadNotificationsSuccess({ notifications })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadNotificationsFail({ error }))
          )
        )
      )
    );
  });

  loadUnreadCount$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUnreadCount),
      switchMap(() =>
        this.notificationsService.getUnreadCount().pipe(
          unwrapClientReposne(),
          map((count) => actions.loadUnreadCountSuccess({ count })),
          catchError(() => of(actions.loadUnreadCountSuccess({ count: 0 })))
        )
      )
    );
  });

  markAsRead$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.markAsRead),
      switchMap(({ id }) =>
        this.notificationsService.markAsRead(id).pipe(
          map(() => actions.markAsReadSuccess({ id })),
          catchError(() => of(actions.markAsReadSuccess({ id })))
        )
      )
    );
  });

  markAllAsRead$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.markAllAsRead),
      switchMap(() =>
        this.notificationsService.markAllAsRead().pipe(
          map(() => actions.markAllAsReadSuccess()),
          catchError(() => of(actions.markAllAsReadSuccess()))
        )
      )
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
          map((count) => actions.loadUnreadCountSuccess({ count })),
          catchError(() => of(actions.loadUnreadCountSuccess({ count: 0 })))
        )
      )
    );
  });
}
