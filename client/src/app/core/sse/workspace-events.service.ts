import {
  EnvironmentProviders,
  Service,
  effect,
  inject,
  provideAppInitializer,
  signal,
  untracked,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { refreshScopesForEntityTypes } from '@core/util/entity-refresh-scopes';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { RealtimeClientIdService } from './realtime-client-id.service';

/** Bursts of remote edits arrive as one event each, and each one costs a round of reloads. */
const REFRESH_DELAY = 250;

interface WorkspaceUpdateFrame {
  scopes?: string[];
}

/** A server that named nothing, or named something unknown, leaves the change unbounded. */
const readChangedScopes = (data: string): Set<RefreshScope> | null => {
  try {
    const frame = JSON.parse(data) as WorkspaceUpdateFrame;
    const entityTypes = frame.scopes ?? [];

    return entityTypes.length ? refreshScopesForEntityTypes(entityTypes) : null;
  } catch {
    return null;
  }
};

/** No view owns the stream, so nothing else would construct the service that opens it. */
export function provideWorkspaceEvents(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(WorkspaceEventsService);
  });
}

/**
 * Holds the workspace event stream open for as long as a workspace is open, so a
 * view that never claimed a realtime group still sees other people's changes.
 */
@Service()
export class WorkspaceEventsService {
  private readonly realtimeClientId = inject(RealtimeClientIdService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly isAuthenticated = inject(SessionService).isAuthenticated;

  private readonly workspaceId = inject(CurrentWorkspaceService).slug;

  /** Presence is reported per group, so a view that shows who else is here claims one. */
  private readonly group = signal<string | null>(null);

  private readonly online = signal<string[]>([]);

  readonly onlineUserIds = this.online.asReadonly();

  private eventSource: EventSource | null = null;
  private isConnected = false;
  private pendingScopes = new Set<RefreshScope>();
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const workspace = this.workspaceId();
      const isReady = this.isAuthenticated() && !!workspace;

      if (!isReady) {
        this.disconnect();

        return;
      }

      this.connect(workspace, this.group() ?? `[workspace] ${workspace}`);
    });
  }

  joinGroup(group: string) {
    this.setGroup(group);
  }

  leaveGroup() {
    this.setGroup(null);
  }

  /* Views claim their group from inside an effect, which must not come to depend on it. */
  private setGroup(group: string | null) {
    const isCurrent = untracked(this.group) === group;

    if (isCurrent) return;

    this.online.set([]);

    this.isConnected = false;
    this.group.set(group);
  }

  private connect(workspace: string, group: string) {
    this.disconnect();

    const params = new URLSearchParams({
      clientId: this.realtimeClientId.value,
      group,
      workspace,
    });

    const url = `${environment.apiEndpoint}api/hubs/board-events?${params.toString()}`;
    const eventSource = new EventSource(url, { withCredentials: true });

    eventSource.addEventListener('message', (event: MessageEvent<string>) => {
      Logger.log('%c[SSE][EVENT] workspace update received', 'color: lime');
      this.requestRefresh(readChangedScopes(event.data));
    });

    eventSource.addEventListener('presence', (event) => {
      Logger.log('%c[SSE][EVENT] presence received', 'color: cyan');
      this.setOnlineUsers(event.data);
    });

    eventSource.onerror = () => {
      Logger.warn('[SSE] Connection error or closed.');
    };

    eventSource.onopen = () => {
      Logger.log('%c[SSE][Connected]', 'color: lime');
      this.handleOpen();
    };

    this.eventSource = eventSource;
  }

  private disconnect() {
    this.isConnected = false;
    this.cancelRefresh();

    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Disconnected]', 'color: orange');
  }

  /* An open that follows an earlier one is a recovered connection, and events were missed while it was down. */
  private handleOpen() {
    if (this.isConnected) {
      this.requestRefresh(null);

      return;
    }

    this.isConnected = true;
  }

  /** A null scope list means the change went unnamed, which only a full refresh covers. */
  private requestRefresh(scopes: Set<RefreshScope> | null) {
    const pending = scopes ?? new Set(allRefreshScopes);

    for (const scope of pending) {
      this.pendingScopes.add(scope);
    }

    if (this.refreshTimer !== null) return;

    this.refreshTimer = setTimeout(() => {
      const requested = this.pendingScopes;

      this.refreshTimer = null;
      this.pendingScopes = new Set();

      this.workspaceRefresh.refresh(requested);
    }, REFRESH_DELAY);
  }

  private cancelRefresh() {
    this.pendingScopes = new Set();

    if (this.refreshTimer === null) return;

    clearTimeout(this.refreshTimer);
    this.refreshTimer = null;
  }

  /** Only a claimed group reports the people looking at the same board. */
  private setOnlineUsers(data: string) {
    const hasGroup = untracked(this.group) !== null;

    if (!hasGroup) return;

    try {
      this.online.set(JSON.parse(data) as string[]);
    } catch {
      Logger.warn('[SSE] Failed to parse presence event.');
    }
  }
}
