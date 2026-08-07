import { Injectable, inject } from '@angular/core';
import { ExportJobProgressEvent } from '@core/models/view-models/export-job-view-model';
import { ImportSessionProgressEvent } from '@core/models/view-models/import-session';
import { selectIsAuthenticated } from '@core/store/auth/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

export interface TransferJobHandlers {
  onExport?: (progress: ExportJobProgressEvent) => void;
  onImport?: (progress: ImportSessionProgressEvent) => void;
}

@Injectable({
  providedIn: 'root',
})
export class TransferJobSseService {
  private readonly store = inject(Store);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  connect(handlers: TransferJobHandlers): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const workspaceId = this.workspaceId();

    if (!workspaceId) {
      return;
    }

    const params = new URLSearchParams({ workspace: workspaceId });
    const url = `${environment.apiEndpoint}api/hubs/transfer-jobs?${params.toString()}`;

    this.eventSource = new EventSource(url, { withCredentials: true });

    this.listen('export-job-progress', handlers.onExport);
    this.listen('import-session-progress', handlers.onImport);

    this.eventSource.onerror = () => {
      Logger.warn('[SSE][Transfers] Connection error or closed.');
    };

    this.eventSource.onopen = () => {
      Logger.log('%c[SSE][Transfers][Connected]', 'color: lime');
    };
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Transfers][Disconnected]', 'color: orange');
  }

  private listen<T>(name: string, handler?: (progress: T) => void): void {
    if (!this.eventSource || !handler) return;

    this.eventSource.addEventListener(name, (event: MessageEvent<string>) => {
      try {
        handler(JSON.parse(event.data) as T);
      } catch {
        Logger.warn(`[SSE][Transfers] failed to parse a ${name} event`);
      }
    });
  }
}
