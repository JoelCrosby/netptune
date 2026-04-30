import { Injectable, inject } from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

@Injectable({
  providedIn: 'root',
})
export class SseService {
  private store = inject(Store);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  connect(group: string, onEvent: () => void): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const params = new URLSearchParams({ group });
    const workspaceId = this.workspaceId();

    if (workspaceId) {
      params.set('workspace', workspaceId);
    }

    const url = `${environment.apiEndpoint}api/hubs/board-events?${params.toString()}`;

    this.eventSource = new EventSource(url, { withCredentials: true });

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
