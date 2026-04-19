import { Action, createReducer, on } from '@ngrx/store';
import { initialState, NotificationsState } from './notifications.model';
import * as actions from './notifications.actions';

const reducer = createReducer(
  initialState,

  on(
    actions.loadNotifications,
    (state): NotificationsState => ({ ...state, loading: true })
  ),

  on(
    actions.loadNotificationsSuccess,
    (state, { notifications }): NotificationsState => ({
      ...state,
      loading: false,
      loaded: true,
      notifications,
      unreadCount: notifications.filter((n) => !n.isRead).length,
    })
  ),

  on(
    actions.loadNotificationsFail,
    (state, { error }): NotificationsState => ({
      ...state,
      loading: false,
      loaded: true,
      loadingError: error,
    })
  ),

  on(
    actions.loadUnreadCountSuccess,
    (state, { count }): NotificationsState => ({ ...state, unreadCount: count })
  ),

  on(
    actions.markAsReadSuccess,
    (state, { id }): NotificationsState => ({
      ...state,
      notifications: state.notifications.map((n) =>
        n.id === id ? { ...n, isRead: true } : n
      ),
      unreadCount: Math.max(0, state.unreadCount - 1),
    })
  ),

  on(
    actions.markAllAsReadSuccess,
    (state): NotificationsState => ({
      ...state,
      notifications: state.notifications.map((n) => ({ ...n, isRead: true })),
      unreadCount: 0,
    })
  ),

  on(
    actions.notificationReceived,
    (state): NotificationsState => ({
      ...state,
      unreadCount: state.unreadCount + 1,
    })
  )
);

export const notificationsReducer = (
  state: NotificationsState | undefined,
  action: Action
): NotificationsState => reducer(state, action);
