import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const loadNotifications = createAsyncAction(
  '[Notifications] Load Notifications',
  {
    success: props<{ notifications: NotificationViewModel[] }>(),
  }
);

export const loadUnreadCount = createAsyncAction(
  '[Notifications] Load Unread Count',
  {
    success: props<{ count: number }>(),
  }
);

export const notificationReceived = createAction(
  '[Notifications] Notification Received',
  props<{ notificationId: number }>()
);

export const markAsRead = createAsyncAction('[Notifications] Mark As Read', {
  init: props<{ id: number }>(),
  success: props<{ id: number }>(),
});

export const markAllAsRead = createAsyncAction(
  '[Notifications] Mark All As Read'
);
