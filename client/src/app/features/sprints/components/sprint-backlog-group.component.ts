import { LowerCasePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskStatus, taskStatusLabels } from '@core/enums/project-task-status';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { assignBacklogTask } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';

export interface BacklogGroup {
  label: string;
  status: TaskStatus;
  tasks: TaskViewModel[];
}

@Component({
  selector: 'app-sprint-backlog-group',
  imports: [
    LowerCasePipe,
    RouterLink,
    DropdownButtonComponent,
    MenuItemComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
    TaskScopeIdComponent,
  ],
  template: `
    <div class="flex flex-col gap-2">
      <div class="flex items-center gap-2">
        <h2 class="text-sm font-semibold tracking-wide uppercase">
          {{ group().label }}
        </h2>
        <span class="bg-muted rounded-full px-2 py-0.5 text-xs font-medium">
          {{ group().tasks.length }}
        </span>
      </div>

      <div class="p-2">
        <app-table
          containerClass="overflow-auto"
          tableClass="min-w-[880px] table-fixed">
          <thead appTableHead [sticky]="true">
            <tr appTableHeaderRow>
              <th class="px-4 py-3">Task</th>
              <th class="w-48 px-4 py-3">Status</th>
              <th class="w-28 px-4 py-3">Priority</th>
              <th class="w-44 px-4 py-3">Project</th>
              <th class="w-68 px-4 py-3">Assign</th>
            </tr>
          </thead>
          <tbody>
            @for (task of group().tasks; track task.id) {
              <tr appTableRow class="bg-card">
                <td class="min-w-0 px-4 py-2.5 align-middle">
                  <div class="flex min-w-0 items-center gap-2">
                    <app-task-scope-id class="flex-none" [id]="task.systemId" />
                    <a
                      class="block min-w-0 truncate font-medium"
                      [routerLink]="['../../tasks', task.systemId]">
                      {{ task.name }}
                    </a>
                  </div>
                </td>
                <td class="px-4 py-2.5 align-middle">
                  <span
                    class="rounded px-1.5 py-0.5 text-xs font-medium"
                    [class]="statusBadgeClass(task.status)">
                    {{ statusLabel(task.status) }}
                  </span>
                </td>
                <td class="px-4 py-2.5 align-middle">
                  @if (task.priority !== null && task.priority !== undefined) {
                    <span
                      class="text-xs font-medium"
                      [class]="priorityClass(task.priority)">
                      {{ priorityLabel(task.priority) }}
                    </span>
                  } @else {
                    <span class="text-muted text-xs">None</span>
                  }
                </td>
                <td class="px-4 py-2.5 align-middle">
                  <span class="text-muted block truncate text-xs">
                    {{ task.projectName }}
                  </span>
                </td>
                <td class="px-4 py-2.5 align-middle">
                  @if (sprints().length > 0) {
                    <app-dropdown-button
                      #assignMenu
                      label="Assign to sprint"
                      buttonClass="w-52 justify-between"
                      color="neutral"
                      xPosition="before"
                      [disabled]="loading()">
                      @for (sprint of sprints(); track sprint.id) {
                        <button
                          app-menu-item
                          type="button"
                          class="min-w-52"
                          (click)="
                            onAssign(task, sprint.id); assignMenu.close()
                          ">
                          <span class="flex min-w-0 flex-col items-start">
                            <span class="max-w-48 truncate font-medium">
                              {{ sprint.name }}
                            </span>
                            <span class="text-muted max-w-48 truncate text-xs">
                              {{ sprint.projectName }}
                            </span>
                          </span>
                        </button>
                      }
                    </app-dropdown-button>
                  } @else {
                    <span class="text-muted text-sm">No sprints available</span>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td appTableEmptyCell colspan="5">
                  No {{ group().label | lowercase }} tasks in the backlog.
                </td>
              </tr>
            }
          </tbody>
        </app-table>
      </div>
    </div>
  `,
})
export class SprintBacklogGroupComponent {
  private store = inject(Store);

  readonly group = input.required<BacklogGroup>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly loading = this.store.selectSignal(selectSprintUpdateLoading);

  onAssign(task: TaskViewModel, sprintId: number) {
    if (!task.id || !sprintId) return;

    this.store.dispatch(assignBacklogTask({ taskId: task.id, sprintId }));
  }

  statusLabel(status: TaskStatus) {
    return taskStatusLabels[status];
  }

  statusBadgeClass(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.new:
        return 'bg-blue-100 text-blue-700';
      case TaskStatus.inProgress:
        return 'bg-yellow-100 text-yellow-700';
      case TaskStatus.complete:
        return 'bg-green-100 text-green-700';
      case TaskStatus.onHold:
        return 'bg-purple-100 text-purple-700';
      default:
        return 'bg-neutral-100 text-neutral-600';
    }
  }

  priorityLabel(priority: TaskPriority) {
    return taskPriorityLabels[priority];
  }

  priorityClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.critical:
        return 'text-red-500';
      case TaskPriority.high:
        return 'text-orange-400';
      case TaskPriority.medium:
        return 'text-yellow-500';
      case TaskPriority.low:
        return 'text-blue-400';
      default:
        return 'text-zinc-400';
    }
  }
}
