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
      ofType(actions.loadNotifications.init, actions.notificationReceived),
      switchMap(() =>
        this.notificationsService.getAll().pipe(
          unwrapClientReposne(),
          map((notifications) =>
            actions.loadNotifications.success({ notifications })
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
