import {
  assertInInjectionContext,
  effect,
  inject,
  untracked,
} from '@angular/core';
import { RefreshScope } from '@core/models/refresh-scope';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';

export interface ReloadableResource {
  reload(): boolean;
}

export function reloadOnRefresh(
  resource: ReloadableResource,
  scopes: readonly RefreshScope[]
): void {
  assertInInjectionContext(reloadOnRefresh);

  const workspaceRefresh = inject(WorkspaceRefreshService);
  const versions = scopes.map((scope) => workspaceRefresh.version(scope));

  let isFirstRun = true;

  effect(() => {
    for (const version of versions) {
      version();
    }

    if (isFirstRun) {
      isFirstRun = false;

      return;
    }

    untracked(() => resource.reload());
  });
}
