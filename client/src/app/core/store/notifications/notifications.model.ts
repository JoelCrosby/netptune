import { HttpErrorResponse } from '@angular/common/http';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';

export const initialState: NotificationsState = {
  notifications: [],
  unreadCount: 0,
  loading: false,
  loaded: false,
};

export interface NotificationsState {
  notifications: NotificationViewModel[];
  unreadCount: number;
  loading: boolean;
  loaded: boolean;
  loadingError?: HttpErrorResponse | Error;
}
