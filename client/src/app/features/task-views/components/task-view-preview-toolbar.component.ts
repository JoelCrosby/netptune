import { Component, input, model } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  DatatableColumn,
  DatatableColumnPreference,
} from '@static/components/datatable/datatable.types';
import { TaskQueryField } from '../models/task-view.models';
import { TaskViewDisplayMenuComponent } from './task-view-display-menu.component';

/**
 * Strip between the query bar and the preview table: how many tasks the query matches on the
 * left, the display controls on the right.
 */
@Component({
  selector: 'app-task-view-preview-toolbar',
  imports: [TaskViewDisplayMenuComponent],
  host: {
    class:
      'border-border bg-card-header flex shrink-0 items-center gap-3 border-x border-t px-4 py-2.5',
  },
  template: `
    <p class="text-sm" role="status" aria-live="polite">
      @if (loading()) {
        <span
          class="text-foreground/38"
          i18n="Shown while the query result count is being recounted">
          Counting…
        </span>
      } @else {
        <span class="text-foreground font-medium">{{ count() }}</span>
        <span
          class="text-foreground/38"
          i18n="Label after the number of tasks a query matches">
          matching tasks
        </span>
      }
    </p>

    <app-task-view-display-menu
      class="ml-auto"
      [columns]="availableColumns()"
      [sortableFields]="sortableFields()"
      [(preferences)]="preferences"
      [(sortBy)]="sortBy"
      [(sortDirection)]="sortDirection" />
  `,
})
export class TaskViewPreviewToolbarComponent {
  readonly loading = input(false);
  readonly count = input(0);
  readonly availableColumns =
    input.required<DatatableColumn<TaskViewModel>[]>();
  readonly sortableFields = input.required<TaskQueryField[]>();

  readonly preferences = model.required<DatatableColumnPreference[]>();
  readonly sortBy = model.required<string>();
  readonly sortDirection = model.required<string>();
}
