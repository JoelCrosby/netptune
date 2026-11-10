import { Service, inject } from '@angular/core';
import {
  PendingSourceControlConnect,
  writePendingSourceControlConnect,
} from '@core/auth/pending-source-control-connect';
import {
  SourceControlProvider,
  SourceControlRepo,
} from '@core/models/source-control';
import { SourceControlService } from '@core/services/source-control.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { requireSuccess } from '@core/util/rxjs-operators';
import { map, tap } from 'rxjs';

// Unlinking a repository or disconnecting its account changes which links tasks show, so every
// write here refreshes the task code links as well as the settings views.
@Service()
export class SourceControlCommandsService {
  private readonly api = inject(SourceControlService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  beginConnect(pending: PendingSourceControlConnect) {
    return this.api.beginConnect(pending.provider).pipe(
      requireSuccess(),
      tap((response) => {
        const redirectUrl = response.payload?.redirectUrl;

        if (!redirectUrl) {
          throw new Error(CONNECT_FAILED);
        }

        writePendingSourceControlConnect(pending);
        window.location.assign(redirectUrl);
      })
    );
  }

  completeTokenConnect(
    provider: SourceControlProvider,
    parameters: Record<string, string>,
    acceptWarnings: boolean
  ) {
    return this.api.completeConnect(provider, parameters, acceptWarnings).pipe(
      requireSuccess(),
      map((response) => {
        const completion = response.payload;

        if (!completion) {
          throw new Error(COMPLETE_FAILED);
        }

        return completion;
      }),
      tap((completion) => {
        if (completion.connection) {
          this.refresh();
        }
      })
    );
  }

  checkConnection(connectionId: number) {
    return this.api.checkConnection(connectionId).pipe(
      requireSuccess(),
      tap(() => this.workspaceRefresh.refresh(['sourceControl']))
    );
  }

  disconnect(connectionId: number) {
    return this.api.disconnect(connectionId).pipe(
      requireSuccess(),
      tap(() => this.refresh())
    );
  }

  sync(connectionId: number) {
    return this.api.sync(connectionId).pipe(
      requireSuccess(),
      tap(() => this.refresh())
    );
  }

  setLinked(repo: SourceControlRepo, isLinked: boolean) {
    return this.api.setLinked(repo.id, isLinked).pipe(
      requireSuccess(),
      tap(() => this.refresh())
    );
  }

  syncHistory(repo: SourceControlRepo) {
    return this.api.syncHistory(repo.id).pipe(
      requireSuccess(),
      tap(() => this.workspaceRefresh.refresh(['sourceControl']))
    );
  }

  private refresh() {
    this.workspaceRefresh.refresh(['sourceControl', 'codeLinks']);
  }
}

const CONNECT_FAILED = $localize`:Shown when starting a source control connection fails:The connection could not be started.`;
const COMPLETE_FAILED = $localize`:Shown when finishing a source control connection fails:The connection could not be completed.`;
