import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { taskPriorityLabels } from '@core/enums/task-priority';
import { DatatableColumnPreference } from '@static/components/datatable/datatable.types';
import { visibleColumnIds } from '../util/task-view-columns';

@Component({
  selector: 'app-task-view-preview-table',
  imports: [],
  host: { class: 'block min-h-0 flex-1 overflow-auto' },
  template: `
    @if (rows().length) {
      <table class="w-full border-collapse text-left text-sm">
        <thead class="bg-card-header sticky top-0 z-10">
          <tr>
            @for (column of visibleIds(); track column) {
              <th
                scope="col"
                class="border-border text-foreground/40 border-b px-4.5 py-2.75 text-[11px] font-semibold tracking-[0.07em] whitespace-nowrap uppercase">
                {{ headerFor(column) }}
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @for (task of rows(); track task.id) {
            <tr class="hover:bg-foreground/3 border-b border-white/4.5">
              @for (column of visibleIds(); track column) {
                <td
                  class="max-w-104 truncate px-4.5 py-2.75"
                  [class.font-medium]="column === 'name'"
                  [class.text-foreground/60]="column !== 'name'"
                  [class.font-semibold]="column === 'systemId'">
                  {{ cell(task, column) }}
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <p class="text-foreground/55 px-6 py-16 text-center text-sm">
        @if (loading()) {
          <span i18n="Shown while a query preview is loading">
            Running the query…
          </span>
        } @else {
          <span i18n="Shown when a query preview matches no tasks">
            No tasks match this query yet.
          </span>
        }
      </p>
    }
  `,
})
export class TaskViewPreviewTableComponent {
  private readonly datePipe = new DatePipe('en-GB');

  readonly rows = input.required<TaskViewModel[]>();
  readonly loading = input(false);
  readonly preferences = input.required<DatatableColumnPreference[]>();

  readonly visibleIds = computed(() => visibleColumnIds(this.preferences()));

  headerFor(columnId: string): string {
    return columnHeaders[columnId] ?? columnId;
  }

  cell(task: TaskViewModel, columnId: string): string {
    switch (columnId) {
      case 'systemId':
        return task.systemId;
      case 'name':
        return task.name;
      case 'status':
        return task.statusName ?? '';
      case 'assignees':
        return task.assignees
          .map((assignee) => assignee.displayName)
          .join(', ');
      case 'priority':
        return task.priority === null || task.priority === undefined
          ? ''
          : taskPriorityLabels[task.priority];
      case 'project':
        return task.projectName;
      case 'sprint':
        return task.sprintName ?? '';
      case 'dueDate':
        return this.formatDate(task.dueDate);
      case 'startDate':
        return this.formatDate(task.startDate);
      case 'updatedAt':
        return this.formatDate(task.updatedAt);
      default:
        return '';
    }
  }

  private formatDate(value: string | Date | null | undefined): string {
    if (!value) return '';

    return this.datePipe.transform(value, 'mediumDate') ?? '';
  }
}

const columnHeaders: Record<string, string> = {
  systemId: $localize`:Column heading for the task reference:Id`,
  name: $localize`:Column heading for the task name:Name`,
  status: $localize`:Column heading for the task status:Status`,
  assignees: $localize`:Column heading for the task assignees:Assignees`,
  priority: $localize`:Column heading for the task priority:Priority`,
  project: $localize`:Column heading for the task project:Project`,
  sprint: $localize`:Column heading for the task sprint:Sprint`,
  dueDate: $localize`:Column heading for the task due date:Due`,
  startDate: $localize`:Column heading for the task start date:Start`,
  updatedAt: $localize`:Column heading for when the task last changed:Updated`,
};
