import { PERMISSIONS } from '@core/auth/permissions';
import { NotificationSubscription } from '@core/models/notification-subscription';
import { permissionResource } from './permission.resource';

export const notificationSubscriptionsResource = () => {
  return permissionResource<NotificationSubscription[]>(
    PERMISSIONS.notifications.read,
    () => ({ url: 'api/notification-subscriptions' }),
    {
      defaultValue: [],
      refreshOn: ['notificationSubscriptions'],
    }
  );
};
