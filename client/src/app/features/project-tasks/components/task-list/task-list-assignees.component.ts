import { Component, computed, inject, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { toggleSelectedAssignee } from '@core/store/tasks/tasks.actions';
import { selectTaskAssigneeOptions } from '@core/store/tasks/tasks.selectors';
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
      emptyLabel="No assignees"
      (optionClicked)="onAssigneeClicked($event)" />
  `,
})
export class TaskListAssigneesComponent {
  private readonly store = inject(Store);

  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);
  readonly storeAssignees = this.store.selectSignal(selectTaskAssigneeOptions);
  readonly assignees = computed(
    () => this.assigneeOptions() ?? this.storeAssignees()
  );

  onAssigneeClicked(option: AvatarFilterOption) {
    this.store.dispatch(toggleSelectedAssignee({ assigneeId: option.id }));
  }
}
