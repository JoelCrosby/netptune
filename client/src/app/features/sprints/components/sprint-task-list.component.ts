import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { removeTaskFromSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { SprintAddTaskFormComponent } from './sprint-add-task-form.component';
import { SprintBacklogPriorityClassPipe } from '../pipes/sprint-backlog-priority-class.pipe';
import { SprintBacklogPriorityLabelPipe } from '../pipes/sprint-backlog-priority-label.pipe';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';

@Component({
  selector: 'app-sprint-task-list',
  imports: [
    RouterLink,
    StrokedButtonComponent,
    TaskScopeIdComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
    SprintAddTaskFormComponent,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogStatusLabelPipe,
    SprintBacklogPriorityClassPipe,
    SprintBacklogPriorityLabelPipe,
  ],
  template: `
    @if (canEditSprintTasks()) {
      <app-sprint-add-task-form [sprintId]="sprint().id!" />
    }

    <app-table>
      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="w-28 px-4 py-3">Key</th>
          <th class="px-4 py-3">Task</th>
          <th class="w-48 px-4 py-3">Project</th>
          <th class="w-40 px-4 py-3">Status</th>
          <th class="w-32 px-4 py-3">Priority</th>
          @if (canEditSprintTasks()) {
            <th class="w-28 px-4 py-3"></th>
          }
        </tr>
      </thead>
      <tbody>
        @for (task of sprint().tasks; track task.id) {
          <tr appTableRow class="bg-card">
            <td class="px-4 py-2.5 align-middle">
              <app-task-scope-id [id]="task.systemId" />
            </td>
            <td class="min-w-64 px-4 py-2.5 align-middle">
              <a
                class="block w-full truncate font-medium hover:underline"
                [routerLink]="['../../tasks', task.systemId]">
                {{ task.name }}
              </a>
            </td>
            <td class="text-muted px-4 py-2.5 align-middle text-sm">
              {{ task.projectName }}
            </td>
            <td class="px-4 py-2.5 align-middle">
              <span
                class="inline-flex items-center rounded px-2 py-0.5 text-center text-xs font-medium"
                [class]="task.status | sprintBacklogStatusBadgeClass">
                {{ task.status | sprintBacklogStatusLabel }}
              </span>
            </td>
            <td class="px-4 py-2.5 align-middle">
              @if (task.priority !== null && task.priority !== undefined) {
                <span
                  class="text-sm font-medium"
                  [class]="task.priority | sprintBacklogPriorityClass">
                  {{ task.priority | sprintBacklogPriorityLabel }}
                </span>
              } @else {
                <span class="text-muted text-sm">None</span>
              }
            </td>

            @if (canEditSprintTasks()) {
              <td class="px-4 py-2.5 text-right align-middle">
                <button
                  app-stroked-button
                  color="primary"
                  type="button"
                  class="h-6 text-xs"
                  [disabled]="updateLoading()"
                  (click)="onRemoveTask(task.id)">
                  Remove
                </button>
              </td>
            }
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell [attr.colspan]="columnCount()">
              <div class="text-muted p-6 text-center text-sm">
                No tasks in this sprint.
              </div>
            </td>
          </tr>
        }
      </tbody>
    </app-table>
  `,
})
export class SprintTaskListComponent {
  private store = inject(Store);

  readonly sprint = input.required<SprintDetailViewModel>();
  readonly canManage = input.required<boolean>();

  readonly sprintStatus = SprintStatus;
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);
  readonly canEditSprintTasks = computed(
    () => this.canManage() && this.sprint().status !== SprintStatus.completed
  );

  columnCount() {
    return this.canEditSprintTasks() ? 6 : 5;
  }

  onRemoveTask(taskId?: number) {
    const sprintId = this.sprint().id;
    if (!sprintId || !taskId) return;
    this.store.dispatch(removeTaskFromSprint({ sprintId, taskId }));
  }
}
