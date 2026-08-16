import { Component, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { TaskListAssigneesComponent } from './task-list-assignees.component';
import { TaskListFilterSeparatorComponent } from './task-list-filter-separator.component';
import { TaskListFlagsComponent } from './task-list-flags.component';
import { TaskListSearchComponent } from './task-list-search.component';
import { TaskListSelectionActionsComponent } from './task-list-selection-actions.component';
import { TaskListStatusComponent } from './task-list-status.component';
import { PERMISSIONS } from '@app/core/auth/permissions';

@Component({
  selector: 'app-task-list-filters',
  imports: [
    TaskListFilterSeparatorComponent,
    TaskListFlagsComponent,
    TaskListAssigneesComponent,
    TaskListSearchComponent,
    TaskListSelectionActionsComponent,
    TaskListStatusComponent,
    TagFilterContainerComponent,
  ],
  template: `
    <div class="mb-3 flex flex-row items-center gap-3">
      <app-task-list-search />
      <app-task-list-filter-separator />
      <app-task-list-assignees [assigneeOptions]="assigneeOptions()" />

      @if (readFlags()) {
        <app-task-list-filter-separator />
        <app-task-list-flags />
      }

      @if (readTags()) {
        <app-task-list-filter-separator />
        <app-tag-filter-container />
      }

      @if (readStatus()) {
        <app-task-list-filter-separator />
        <app-task-list-status />
      }

      <app-task-list-selection-actions class="ml-auto" />
    </div>
  `,
})
export class TaskListFiltersComponent {
  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);

  readStatus = hasPermission(PERMISSIONS.statuses.read);

  readTags = hasPermission(PERMISSIONS.tags.read);

  readFlags = hasPermission(PERMISSIONS.flags.read);
}
