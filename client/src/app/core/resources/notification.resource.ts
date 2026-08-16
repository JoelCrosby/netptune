import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { Page } from '../models/pagination';
import { NotificationViewModel } from '../models/view-models/notification-view-model';
import { permissionResource } from './permission.resource';

const RECENT_PAGE_SIZE = 10;

export const recentNotificationsResource = () => {
  return permissionResource<NotificationViewModel[]>(
    PERMISSIONS.notifications.read,
    () => ({
      url: 'api/notifications',
      params: { page: 1, pageSize: RECENT_PAGE_SIZE },
    }),
    {
      defaultValue: [],
      refreshOn: ['notifications'],
      parse: (response) => {
        return (
          (response as ClientResponse<Page<NotificationViewModel>>).payload
            ?.items ?? []
        );
      },
    }
  );
};

export const unreadNotificationCountResource = () => {
  return permissionResource<number>(
    PERMISSIONS.notifications.read,
    () => ({ url: 'api/notifications/unread-count' }),
    {
      defaultValue: 0,
      refreshOn: ['notifications'],
      parse: (response) => (response as ClientResponse<number>).payload ?? 0,
    }
  );
};
