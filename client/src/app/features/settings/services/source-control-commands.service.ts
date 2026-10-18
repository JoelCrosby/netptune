import { Service, inject } from '@angular/core';
import { SourceControlRepo } from '@core/models/source-control';
import { SourceControlService } from '@core/services/source-control.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { requireSuccess } from '@core/util/rxjs-operators';
import { tap } from 'rxjs';

// Unlinking a repository or disconnecting its account changes which links tasks show, so every
// write here refreshes the task code links as well as the settings views.
@Service()
export class SourceControlCommandsService {
  private readonly api = inject(SourceControlService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

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
