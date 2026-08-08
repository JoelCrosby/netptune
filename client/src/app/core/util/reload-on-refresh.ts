import {
  assertInInjectionContext,
  effect,
  inject,
  Signal,
  untracked,
} from '@angular/core';
import { RefreshScope } from '@core/models/refresh-scope';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

export interface ReloadableResource {
  reload(): boolean;
}

export function onWorkspaceRefresh(
  scopes: readonly RefreshScope[],
  onRefresh: () => void
): void {
  assertInInjectionContext(onWorkspaceRefresh);

  const workspaceRefresh = inject(WorkspaceRefreshService);
  const versions = scopes.map((scope) => workspaceRefresh.version(scope));

  onChange(versions, onRefresh);
}

export function reloadOnRefresh(
  resource: ReloadableResource,
  scopes: readonly RefreshScope[]
): void {
  assertInInjectionContext(reloadOnRefresh);

  onWorkspaceRefresh(scopes, () => resource.reload());
}

/**
 * A resource asks for the workspace through a header rather than a parameter, so
 * switching workspace leaves it showing the one before.
 */
export function reloadOnWorkspaceChange(resource: ReloadableResource): void {
  assertInInjectionContext(reloadOnWorkspaceChange);

  const store = inject(Store);
  const workspaceIdentifier = store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  onChange([workspaceIdentifier], () => resource.reload());
}

/** The first run is the value the caller already has, so only later ones are changes. */
function onChange(
  sources: readonly Signal<unknown>[],
  onChanged: () => void
): void {
  let isFirstRun = true;

  effect(() => {
    for (const source of sources) {
      source();
    }

    if (isFirstRun) {
      isFirstRun = false;

      return;
    }

    untracked(onChanged);
  });
}
