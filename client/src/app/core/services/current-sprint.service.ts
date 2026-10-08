import { inject, Service, signal } from '@angular/core';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { onChange } from '@core/util/signals';

@Service()
export class CurrentSprintService {
  private readonly open = signal<SprintViewModel | undefined>(undefined);

  readonly sprint = this.open.asReadonly();

  constructor() {
    // The guard switches workspace before the old view is torn down.
    onChange(inject(CurrentWorkspaceService).id, () => this.set(undefined));
  }

  set(sprint: SprintViewModel | undefined) {
    this.open.set(sprint);
  }

  clearIfCurrent(sprintId: number) {
    this.open.update((current) => {
      return current?.id === sprintId ? undefined : current;
    });
  }
}
