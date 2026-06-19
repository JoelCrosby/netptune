import { Component, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { TaskListAssigneesComponent } from './task-list-assignees.component';
import { TaskListFilterSeparatorComponent } from './task-list-filter-separator.component';
import { TaskListSearchComponent } from './task-list-search.component';
import { TaskListStatusComponent } from './task-list-status.component';
import { TaskListTagsComponent } from './task-list-tags.component';

@Component({
  selector: 'app-task-list-filters',
  imports: [
    TaskListFilterSeparatorComponent,
    TaskListAssigneesComponent,
    TaskListSearchComponent,
    TaskListStatusComponent,
    TaskListTagsComponent,
  ],
  template: `
    <div class="mb-3 flex flex-row items-center gap-3">
      <app-task-list-search />
      <app-task-list-filter-separator />
      <app-task-list-assignees [assigneeOptions]="assigneeOptions()" />
      <app-task-list-filter-separator />
      <app-task-list-tags />
      <app-task-list-filter-separator />
      <app-task-list-status />
    </div>
  `,
})
export class TaskListFiltersComponent {
  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);
}
