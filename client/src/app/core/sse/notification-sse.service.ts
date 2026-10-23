import {
  EnvironmentProviders,
  Service,
  effect,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { ReconnectingEventSource } from './reconnecting-event-source';

/** No view owns the stream, so nothing else would construct the service that opens it. */
export function provideNotificationEvents(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(NotificationSseService);
  });
}

@Service()
export class NotificationSseService {
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private eventSource: ReconnectingEventSource | null = null;

  private readonly isAuthenticated = inject(SessionService).isAuthenticated;

  private readonly workspaceId = inject(CurrentWorkspaceService).slug;

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

    const eventSource = new ReconnectingEventSource(
      'Notifications',
      url,
      (source) => {
        source.addEventListener('message', () => {
          Logger.log(
            '%c[SSE][Notifications] notification received',
            'color: lime'
          );
          this.workspaceRefresh.refresh(['notifications']);
        });

        source.onerror = () => {
          Logger.warn('[SSE][Notifications] Connection error or closed.');
        };

        source.onopen = () => {
          Logger.log('%c[SSE][Notifications][Connected]', 'color: lime');
        };
      }
    );

    eventSource.open();

    this.eventSource = eventSource;
  }

  private disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Notifications][Disconnected]', 'color: orange');
  }
}
