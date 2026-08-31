import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  NotificationScope,
  NotificationSubscription,
  UpsertNotificationSubscriptionRequest,
} from '@core/models/notification-subscription';
import { notificationSubscriptionsResource } from '@core/resources/notification-subscription.resource';
import { unwrapClientResponse } from '@core/util/rxjs-operators';

@Service()
export class NotificationSubscriptionsService {
  private readonly http = inject(HttpClient);

  // One resource for the whole app: a board draws a bell per column, and each of those would
  // otherwise fetch the same list.
  private readonly resource = notificationSubscriptionsResource();

  readonly subscriptions = this.resource.value;
  readonly loading = this.resource.isLoading;

  find(scope: NotificationScope, scopeEntityId: number) {
    return this.subscriptions().find((subscription) => {
      return (
        subscription.scope === scope &&
        subscription.scopeEntityId === scopeEntityId
      );
    });
  }

  upsert(request: UpsertNotificationSubscriptionRequest) {
    return this.http
      .put<ClientResponse<NotificationSubscription>>(
        'api/notification-subscriptions',
        request
      )
      .pipe(unwrapClientResponse());
  }

  delete(id: number) {
    return this.http.delete<ClientResponse>(
      `api/notification-subscriptions/${id}`
    );
  }
}
