import { Service, inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ExportJobProgressEvent } from '@core/models/view-models/export-job-view-model';
import { ImportSessionProgressEvent } from '@core/models/view-models/import-session';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { ReconnectingEventSource } from './reconnecting-event-source';

export interface TransferJobHandlers {
  onExport?: (progress: ExportJobProgressEvent) => void;
  onImport?: (progress: ImportSessionProgressEvent) => void;
}

@Service()
export class TransferJobSseService {
  private eventSource: ReconnectingEventSource | null = null;

  private readonly isAuthenticated = inject(SessionService).isAuthenticated;
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;

  connect(handlers: TransferJobHandlers): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const workspaceId = this.workspaceId();

    if (!workspaceId) {
      return;
    }

    const params = new URLSearchParams({ workspace: workspaceId });
    const url = `${environment.apiEndpoint}api/hubs/transfer-jobs?${params.toString()}`;

    this.eventSource = new ReconnectingEventSource(
      'Transfers',
      url,
      (source) => {
        this.listen(source, 'export-job-progress', handlers.onExport);
        this.listen(source, 'import-session-progress', handlers.onImport);

        source.onerror = () => {
          Logger.warn('[SSE][Transfers] Connection error or closed.');
        };

        source.onopen = () => {
          Logger.log('%c[SSE][Transfers][Connected]', 'color: lime');
        };
      }
    );

    this.eventSource.open();
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Transfers][Disconnected]', 'color: orange');
  }

  private listen<T>(
    source: EventSource,
    name: string,
    handler?: (progress: T) => void
  ): void {
    if (!handler) return;

    source.addEventListener(name, (event: MessageEvent<string>) => {
      try {
        handler(JSON.parse(event.data) as T);
      } catch {
        Logger.warn(`[SSE][Transfers] failed to parse a ${name} event`);
      }
    });
  }
}
