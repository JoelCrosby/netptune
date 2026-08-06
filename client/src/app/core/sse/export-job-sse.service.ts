import { Injectable, inject } from '@angular/core';
import { ExportJobProgressEvent } from '@core/models/view-models/export-job-view-model';
import { selectIsAuthenticated } from '@core/store/auth/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

@Injectable({
  providedIn: 'root',
})
export class ExportJobSseService {
  private readonly store = inject(Store);
  private eventSource: EventSource | null = null;

  private readonly isAuthenticated = this.store.selectSignal(
    selectIsAuthenticated
  );
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  connect(onProgress: (progress: ExportJobProgressEvent) => void): void {
    if (!this.isAuthenticated()) return;

    this.disconnect();

    const workspaceId = this.workspaceId();

    if (!workspaceId) {
      return;
    }

    const params = new URLSearchParams({ workspace: workspaceId });
    const url = `${environment.apiEndpoint}api/hubs/export-jobs?${params.toString()}`;

    this.eventSource = new EventSource(url, { withCredentials: true });

    this.eventSource.addEventListener(
      'export-job-progress',
      (event: MessageEvent<string>) => {
        try {
          const progress = JSON.parse(event.data) as ExportJobProgressEvent;
          onProgress(progress);
        } catch {
          Logger.warn('[SSE][Exports] failed to parse a progress event');
        }
      }
    );

    this.eventSource.onerror = () => {
      Logger.warn('[SSE][Exports] Connection error or closed.');
    };

    this.eventSource.onopen = () => {
      Logger.log('%c[SSE][Exports][Connected]', 'color: lime');
    };
  }

  disconnect(): void {
    if (!this.eventSource) return;

    this.eventSource.close();
    this.eventSource = null;

    Logger.log('%c[SSE][Exports][Disconnected]', 'color: orange');
  }
}
