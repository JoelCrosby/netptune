import { Injectable, inject } from '@angular/core';
import {
  selectAuthTokenWithWorkspaceId,
  selectIsAuthenticated,
} from '@core/auth/store/auth.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

@Injectable({
  providedIn: 'root',
})
export class SseService {
  private store = inject(Store);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );

  connect(group: string, onEvent: () => void): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const tokenSignal = this.store.selectSignal(selectAuthTokenWithWorkspaceId);
    const { token, workspaceId } = tokenSignal();

    if (!token) {
      console.error('[SSE] Cannot connect: auth token not present.');
      return;
    }

    const params = new URLSearchParams({ access_token: token, group });

    if (workspaceId) {
      params.set('workspace', workspaceId);
    }

    const url = `${environment.apiEndpoint}api/hubs/board-events?${params.toString()}`;

    this.eventSource = new EventSource(url);

    this.eventSource.addEventListener('message', () => {
      Logger.log('%c[SSE][EVENT] board-update received', 'color: lime');
      onEvent();
    });

    this.eventSource.onerror = () => {
      Logger.warn('[SSE] Connection error or closed.');
    };

    this.eventSource.onopen = () => {
      Logger.log('%c[SSE][Connected]', 'color: lime');
    };
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Disconnected]', 'color: orange');
  }
}
