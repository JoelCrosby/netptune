import { computed, inject, Injectable, untracked } from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';

/** The sprint slice of {@link TaskFilterService}, which several views read on its own. */
@Injectable({ providedIn: 'root' })
export class SprintFilterService {
  private readonly taskFilters = inject(TaskFilterService);

  readonly sprintId = computed(() => this.taskFilters.filters().sprintId);

  set(sprintId?: number) {
    this.taskFilters.update({ sprintId });
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
