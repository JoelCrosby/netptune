import {
  EnvironmentProviders,
  Injectable,
  effect,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';

/** No view owns the stream, so nothing else would construct the service that opens it. */
export function provideNotificationEvents(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(NotificationSseService);
  });
}

@Injectable({
  providedIn: 'root',
})
export class NotificationSseService {
  private store = inject(Store);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );

  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  constructor() {
    effect(() => {
      const workspace = this.workspaceId();
      const isReady = this.isAuthenticated() && !!workspace;

      if (!isReady) {
        this.disconnect();

        return;
      }

      this.connect(workspace);
    });
  }

  private connect(workspace: string): void {
    this.disconnect();

    const params = new URLSearchParams({ workspace });
    const url = `${environment.apiEndpoint}api/hubs/notifications?${params.toString()}`;

    const eventSource = new EventSource(url, { withCredentials: true });

    eventSource.addEventListener('message', () => {
      Logger.log('%c[SSE][Notifications] notification received', 'color: lime');
      this.workspaceRefresh.refresh(['notifications']);
    });

    eventSource.onerror = () => {
      Logger.warn('[SSE][Notifications] Connection error or closed.');
    };

    eventSource.onopen = () => {
      Logger.log('%c[SSE][Notifications][Connected]', 'color: lime');
    };

    this.eventSource = eventSource;
  }

  private disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Notifications][Disconnected]', 'color: orange');
  }
}
