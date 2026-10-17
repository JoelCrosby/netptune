import { Service, inject } from '@angular/core';
import { SourceControlProvider } from '@core/models/source-control';
import { SourceControlService } from '@core/services/source-control.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { requireSuccess } from '@core/util/rxjs-operators';
import { tap } from 'rxjs';

@Service()
export class SourceControlIdentityCommandsService {
  private readonly api = inject(SourceControlService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  add(provider: SourceControlProvider, username: string) {
    return this.api.addIdentity(provider, username).pipe(
      requireSuccess(),
      tap(() => this.refresh())
    );
  }

  remove(identityId: number) {
    return this.api.removeIdentity(identityId).pipe(
      requireSuccess(),
      tap(() => this.refresh())
    );
  }

  private refresh() {
    this.workspaceRefresh.refresh(['codeLinks']);
  }
}
