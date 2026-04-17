import { createSelector } from '@ngrx/store';
import { selectNotificationsFeature } from '../../core.state';

export const selectNotifications = createSelector(
  selectNotificationsFeature,
  (state) => state.notifications
);

export const selectUnreadCount = createSelector(
  selectNotificationsFeature,
  (state) => state.unreadCount
);

export const selectNotificationsLoading = createSelector(
  selectNotificationsFeature,
  (state) => state.loading
);

export const selectNotificationsLoaded = createSelector(
  selectNotificationsFeature,
  (state) => state.loaded
);
