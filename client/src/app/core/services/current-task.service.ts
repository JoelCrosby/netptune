import { inject, Service, signal } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { onChange } from '@core/util/signals';

@Service()
export class CurrentTaskService {
  private readonly open = signal<TaskViewModel | undefined>(undefined);

  readonly task = this.open.asReadonly();

  constructor() {
    // The guard switches workspace before the old view is torn down.
    onChange(inject(CurrentWorkspaceService).id, () => this.set(undefined));
  }

  set(task: TaskViewModel | undefined) {
    this.open.set(task);
  }

  clearIfCurrent(systemId: string) {
    this.open.update((current) => {
      return current?.systemId === systemId ? undefined : current;
    });
  }
}
