import { Injectable, inject } from '@angular/core';
import { selectAuthTokenWithWorkspaceId, selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import * as notificationActions from '@core/store/notifications/notifications.actions';

interface NotificationEvent {
  notificationId: number;
  isRead: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationSseService {
  private store = inject(Store);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);

  connect(): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const tokenSignal = this.store.selectSignal(selectAuthTokenWithWorkspaceId);
    const { token, workspaceId } = tokenSignal();

    if (!token) {
      console.error('[SSE][Notifications] Cannot connect: auth token not present.');
      return;
    }

    const params = new URLSearchParams({ access_token: token });

    if (workspaceId) {
      params.set('workspace', workspaceId);
    }

    const url = `${environment.apiEndpoint}api/hubs/notifications?${params.toString()}`;

    this.eventSource = new EventSource(url);

    this.eventSource.addEventListener('message', (event: MessageEvent<string>) => {
      try {
        const notification = JSON.parse(event.data) as NotificationEvent;
        Logger.log('%c[SSE][Notifications] notification received', 'color: lime', notification);
        this.store.dispatch(notificationActions.notificationReceived({ notificationId: notification.notificationId }));
      } catch {
        Logger.warn('[SSE][Notifications] failed to parse notification event');
      }
    });

    this.eventSource.onerror = () => {
      Logger.warn('[SSE][Notifications] Connection error or closed.');
    };

    this.eventSource.onopen = () => {
      Logger.log('%c[SSE][Notifications][Connected]', 'color: lime');
    };
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Notifications][Disconnected]', 'color: orange');
  }
}
