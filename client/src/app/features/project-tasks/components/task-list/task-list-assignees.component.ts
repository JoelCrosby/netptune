import { Component, computed, inject, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { selectTaskAssignees } from '@core/store/tasks/tasks.selectors';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { Store } from '@ngrx/store';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';

@Component({
  selector: 'app-task-list-assignees',
  imports: [AvatarFilterComponent],
  template: `
    <app-avatar-filter
      [options]="assignees()"
      i18n-emptyLabel="Shown when a task has nobody assigned"
      emptyLabel="No assignees"
      (optionClicked)="onAssigneeClicked($event)" />
  `,
})
export class TaskListAssigneesComponent {
  private readonly store = inject(Store);

  private readonly filterRoute = taskFilterRoute();

  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);

  private readonly selected = computed(
    () => new Set(this.filterRoute.filters().users ?? [])
  );

  private readonly loadedAssignees =
    this.store.selectSignal(selectTaskAssignees);

  readonly assignees = computed<Selected<AssigneeViewModel>[]>(() => {
    const selected = this.selected();
    const options =
      this.assigneeOptions() ??
      this.loadedAssignees().map((assignee) => ({
        ...assignee,
        selected: false,
      }));

    return options.map((option) => ({
      ...option,
      selected: selected.has(option.id),
    }));
  });

  onAssigneeClicked(option: AvatarFilterOption) {
    const selected = new Set(this.selected());

    if (selected.has(option.id)) {
      selected.delete(option.id);
    } else {
      selected.add(option.id);
    }

    this.filterRoute.set('users', [...selected]);
  }
}
