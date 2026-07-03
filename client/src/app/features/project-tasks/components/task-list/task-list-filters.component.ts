import { Component, inject, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { TaskListAssigneesComponent } from './task-list-assignees.component';
import { TaskListFilterSeparatorComponent } from './task-list-filter-separator.component';
import { TaskListSearchComponent } from './task-list-search.component';
import { TaskListSelectionActionsComponent } from './task-list-selection-actions.component';
import { TaskListStatusComponent } from './task-list-status.component';
import { Store } from '@ngrx/store';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';

@Component({
  selector: 'app-task-list-filters',
  imports: [
    TaskListFilterSeparatorComponent,
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
  readonly store = inject(Store);

  readStatus = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );
}
