import { computed, Service } from '@angular/core';
import { retainWhileLoading } from '@core/resources/stable.resource';
import { workspacesResource } from '@core/resources/workspace.resource';

@Service()
export class WorkspaceListService {
  private readonly resource = workspacesResource();

  private readonly held = retainWhileLoading(this.resource);

  readonly workspaces = computed(() => this.held() ?? []);
  readonly loaded = computed(() => this.held() !== undefined);
  readonly loadError = this.resource.error;

  readonly loading = computed(() => {
    return this.resource.isLoading() && !this.loaded();
  });

  reload() {
    this.resource.reload();
  }

  clear() {
    this.held.set(undefined);
  }
}
