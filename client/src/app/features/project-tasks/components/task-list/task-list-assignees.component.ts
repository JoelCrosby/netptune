import { Component, computed, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { taskFilterRoute } from '@core/router/task-filter-route';
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
  private readonly filterRoute = taskFilterRoute();

  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);

  private readonly selected = computed(
    () => new Set(this.filterRoute.filters().users ?? [])
  );

  private readonly workspaceUsers = workspaceUsersResource();

  private readonly loadedAssignees = computed<AssigneeViewModel[]>(() =>
    this.workspaceUsers().map((user) => ({
      id: user.id,
      displayName: user.displayName,
      pictureUrl: user.pictureUrl ?? '',
      isServiceAccount: user.isServiceAccount ?? false,
    }))
  );

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
