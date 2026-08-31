import { Service, inject } from '@angular/core';
import {
  NotificationScope,
  NotificationSubscription,
} from '@core/models/notification-subscription';
import { NotificationSubscriptionsService } from '@core/services/notification-subscriptions.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { Observable, catchError, map, of, tap } from 'rxjs';

@Service()
export class NotificationSubscriptionCommandsService {
  private readonly subscriptionsApi = inject(NotificationSubscriptionsService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);

  /** Emits whether the change was saved, so callers can undo what they showed optimistically. */
  setEvents(
    scope: NotificationScope,
    scopeEntityId: number,
    events: number
  ): Observable<boolean> {
    const saved = this.subscriptionsApi.upsert({
      scope,
      scopeEntityId,
      events,
    });

    return this.settle(saved, this.subscribeFailedMessage());
  }

  unsubscribe(subscription: NotificationSubscription): Observable<boolean> {
    const removed = this.subscriptionsApi.delete(subscription.id);

    return this.settle(removed, this.unsubscribeFailedMessage());
  }

  private settle(command: Observable<unknown>, failureMessage: string) {
    return command.pipe(
      map(() => true),
      catchError(() => {
        this.snackbar.error(failureMessage);

        return of(false);
      }),
      tap((wasSaved) => {
        if (wasSaved) this.refresh();
      })
    );
  }

  private refresh() {
    this.workspaceRefresh.refresh(['notificationSubscriptions']);
  }

  private subscribeFailedMessage() {
    return $localize`:Error shown when a notification subscription could not be saved:Your notification settings for that could not be saved.`;
  }

  private unsubscribeFailedMessage() {
    return $localize`:Error shown when a notification subscription could not be removed:You could not be unsubscribed from that.`;
  }
}
