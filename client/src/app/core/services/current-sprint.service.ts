import { Service, signal } from '@angular/core';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';

@Service()
export class CurrentSprintService {
  private readonly open = signal<SprintViewModel | undefined>(undefined);

  readonly sprint = this.open.asReadonly();

  set(sprint: SprintViewModel | undefined) {
    this.open.set(sprint);
  }

  clearIfCurrent(sprintId: number) {
    this.open.update((current) => {
      return current?.id === sprintId ? undefined : current;
    });
  }
}
