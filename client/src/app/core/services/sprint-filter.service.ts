import { computed, inject, Service, untracked } from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';

export interface SprintFilterSprint {
  id: number;
  projectId: number;
}

/** The project and sprint slice of {@link TaskFilterService}, which several views read on its own. */
@Service()
export class SprintFilterService {
  private readonly taskFilters = inject(TaskFilterService);

  readonly projectId = computed(() => this.taskFilters.filters().projectId);

  readonly sprintId = computed(() => this.taskFilters.filters().sprintId);

  setProject(projectId: number | undefined, sprints: SprintFilterSprint[]) {
    const sprintId = untracked(this.sprintId);
    const sprint = sprints.find((candidate) => candidate.id === sprintId);
    const isElsewhere =
      projectId !== undefined && !!sprint && sprint.projectId !== projectId;

    this.taskFilters.update({
      projectId,
      sprintId: isElsewhere ? undefined : sprintId,
    });
  }

  set(sprint?: SprintFilterSprint) {
    const projectId = sprint?.projectId ?? untracked(this.projectId);

    this.taskFilters.update({ sprintId: sprint?.id, projectId });
  }

  clear() {
    this.set(undefined);
  }

  /** A sprint that was deleted, or is no longer active, cannot stay the filter. */
  clearIfSelected(sprintId: number) {
    if (untracked(this.sprintId) !== sprintId) return;

    this.clear();
  }
}
