import { assertInInjectionContext, computed, inject } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { RefreshScope } from '@core/models/refresh-scope';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { onChange } from '@core/util/signals';

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
  const combined = computed(() => versions.map((version) => version()));

  onChange(combined, () => onRefresh());
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

  const workspaceIdentifier = inject(CurrentWorkspaceService).slug;

  onChange(workspaceIdentifier, () => resource.reload());
}
