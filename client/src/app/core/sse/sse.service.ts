import { Injectable, inject } from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { RealtimeClientIdService } from './realtime-client-id.service';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable({
  providedIn: 'root',
})
export class SseService {
  private store = inject(Store);
  private readonly realtimeClientId = inject(RealtimeClientIdService);
  private eventSource?: EventSource;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  connect(
    group: string,
    onEvent: () => void,
    onPresence?: (userIds: string[]) => void
  ): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const workspaceId = this.workspaceId();

    if (!workspaceId) {
      return;
    }

    const params = new URLSearchParams({
      clientId: this.realtimeClientId.value,
      group,
      workspace: workspaceId,
    });

    const url = `${environment.apiEndpoint}api/hubs/board-events?${params.toString()}`;

    this.eventSource = new EventSource(url, { withCredentials: true });

    this.eventSource.addEventListener('message', () => {
      Logger.log('%c[SSE][EVENT] board-update received', 'color: lime');
      onEvent();
    });

    this.eventSource.addEventListener('presence', (event) => {
      Logger.log('%c[SSE][EVENT] presence received', 'color: cyan');

      if (!onPresence) return;

      try {
        const userIds = JSON.parse(event.data) as string[];
        onPresence(userIds);
      } catch {
        Logger.warn('[SSE] Failed to parse presence event.');
      }
    });

    this.eventSource.onerror = () => {
      Logger.warn('[SSE] Connection error or closed.');
    };

    this.eventSource.onopen = () => {
      Logger.log('%c[SSE][Connected]', 'color: lime');
      onEvent();
    };
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = undefined;

    Logger.log('%c[SSE][Disconnected]', 'color: orange');
  }
}
