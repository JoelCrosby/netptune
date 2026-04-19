import { HttpErrorResponse } from '@angular/common/http';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { createAction, props } from '@ngrx/store';

export const loadNotifications = createAction(
  '[Notifications] Load Notifications'
);

export const loadNotificationsSuccess = createAction(
  '[Notifications] Load Notifications Success',
  props<{ notifications: NotificationViewModel[] }>()
);

export const loadNotificationsFail = createAction(
  '[Notifications] Load Notifications Fail',
  props<{ error: HttpErrorResponse }>()
);

export const loadUnreadCount = createAction(
  '[Notifications] Load Unread Count'
);

export const loadUnreadCountSuccess = createAction(
  '[Notifications] Load Unread Count Success',
  props<{ count: number }>()
);

export const notificationReceived = createAction(
  '[Notifications] Notification Received',
  props<{ notificationId: number }>()
);

export const markAsRead = createAction(
  '[Notifications] Mark As Read',
  props<{ id: number }>()
);

export const markAsReadSuccess = createAction(
  '[Notifications] Mark As Read Success',
  props<{ id: number }>()
);

export const markAllAsRead = createAction('[Notifications] Mark All As Read');

export const markAllAsReadSuccess = createAction(
  '[Notifications] Mark All As Read Success'
);
