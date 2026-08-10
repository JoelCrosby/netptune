import { computed, Service, signal } from '@angular/core';
import { Workspace } from '@core/models/workspace';

@Service()
export class CurrentWorkspaceService {
  private readonly current = signal<Workspace | undefined>(undefined);

  readonly workspace = this.current.asReadonly();
  readonly slug = computed(() => this.workspace()?.slug);
  readonly id = computed(() => this.workspace()?.id);

  set(workspace: Workspace | undefined) {
    this.current.set(workspace);
  }

  /** Keeps the open workspace in step with an edit or a rename of the same one. */
  apply(workspace: Workspace) {
    this.current.update((current) => {
      return current?.id === workspace.id ? workspace : current;
    });
  }

  clearIfCurrent(workspace: Workspace) {
    this.current.update((current) => {
      return current?.id === workspace.id ? undefined : current;
    });
  }
}
