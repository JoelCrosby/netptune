import { LowerCasePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskStatus } from '@core/enums/project-task-status';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { assignBacklogTask } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
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
import { SprintBacklogPriorityClassPipe } from '../pipes/sprint-backlog-priority-class.pipe';
import { SprintBacklogPriorityLabelPipe } from '../pipes/sprint-backlog-priority-label.pipe';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';

export interface BacklogGroup {
  label: string;
  status: TaskStatus;
  tasks: TaskViewModel[];
}

@Component({
  selector: 'app-sprint-backlog-group',
  imports: [
    LowerCasePipe,
    SprintBacklogStatusLabelPipe,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogPriorityLabelPipe,
    SprintBacklogPriorityClassPipe,
    RouterLink,
    AvatarComponent,
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
          tableClass="min-w-[1040px] table-fixed">
          <thead appTableHead [sticky]="true">
            <tr appTableHeaderRow>
              <th class="px-4 py-3">Task</th>
              <th class="w-32 px-4 py-3">Status</th>
              <th class="w-18 px-4 py-3">Priority</th>
              <th class="w-32 px-4 py-3">Project</th>
              <th class="w-28 px-4 py-3">Assignees</th>
              <th class="w-58 px-4 py-3">Assign</th>
            </tr>
          </thead>
          <tbody>
            @for (task of group().tasks; track task.id) {
              <tr appTableRow class="bg-card">
                <td class="min-w-0 px-4 py-2.5 align-middle">
                  <div class="flex min-w-0 items-center gap-2">
                    <app-task-scope-id
                      class="text-xs2 flex-none"
                      [id]="task.systemId" />
                    <a
                      class="block min-w-0 truncate font-medium"
                      [routerLink]="['../../tasks', task.systemId]">
                      {{ task.name }}
                    </a>
                  </div>
                </td>
                <td class="block truncate px-4 py-2.5 align-middle">
                  <span
                    class="truncate rounded px-1.5 py-0.5 text-xs font-medium"
                    [class]="task.status | sprintBacklogStatusBadgeClass">
                    {{ task.status | sprintBacklogStatusLabel }}
                  </span>
                </td>
                <td class="px-4 py-2.5 align-middle">
                  @if (task.priority !== null && task.priority !== undefined) {
                    <span
                      class="text-xs font-medium"
                      [class]="task.priority | sprintBacklogPriorityClass">
                      {{ task.priority | sprintBacklogPriorityLabel }}
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
                  @if (task.assignees.length) {
                    <div
                      class="flex max-w-32 items-center -space-x-2 overflow-hidden">
                      @for (assignee of task.assignees; track assignee.id) {
                        <app-avatar
                          class="ring-card rounded-full ring-2"
                          size="sm"
                          [name]="assignee.displayName"
                          [imageUrl]="assignee.pictureUrl" />
                      }
                    </div>
                  } @else {
                    <span class="text-muted text-sm">Unassigned</span>
                  }
                </td>
                <td class="px-4 py-1 align-middle">
                  @if (sprints().length > 0) {
                    <app-dropdown-button
                      #assignMenu
                      label="Assign to sprint"
                      buttonClass="w-42 h-7 text-xs justify-between"
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
                <td appTableEmptyCell colspan="6">
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
}
