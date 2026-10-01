import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { Page } from '../models/pagination';
import { NotificationViewModel } from '../models/view-models/notification-view-model';
import { permissionResource } from './permission.resource';

const UNREAD_PAGE_SIZE = 10;

export const unreadNotificationsResource = () => {
  return permissionResource<
    NotificationViewModel[],
    ClientResponse<Page<NotificationViewModel>>
  >({
    permission: PERMISSIONS.notifications.read,
    request: () => ({
      url: 'api/notifications',
      params: { page: 1, pageSize: UNREAD_PAGE_SIZE, unreadOnly: true },
    }),
    defaultValue: [],
    refreshOn: ['notifications'],
    parse: (response) => response.payload?.items ?? [],
  });
};

export const unreadNotificationCountResource = () => {
  return permissionResource<number>({
    permission: PERMISSIONS.notifications.read,
    request: () => ({ url: 'api/notifications/unread-count' }),
    defaultValue: 0,
    refreshOn: ['notifications'],
    parse: (response) => response.payload ?? 0,
  });
};
