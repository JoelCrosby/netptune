import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskStatus, taskStatusLabels } from '@core/enums/project-task-status';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { removeTaskFromSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SprintAddTaskFormComponent } from './sprint-add-task-form.component';

@Component({
  selector: 'app-sprint-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    CardComponent,
    StrokedButtonComponent,
    TaskScopeIdComponent,
    SprintAddTaskFormComponent,
  ],
  template: `
    <div class="bg-board-group p-2">
      <app-card class="min-h-0! p-0!">
        @if (canManage() && sprint().status !== sprintStatus.completed) {
          <app-sprint-add-task-form [sprintId]="sprint().id!" />
        }

        @for (task of sprint().tasks; track task.id) {
          <div
            class="border-border flex items-center justify-between gap-4 border-b p-4 last:border-b-0">
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <app-task-scope-id [id]="task.systemId" />
                <a
                  class="truncate font-medium"
                  [routerLink]="['../../tasks', task.systemId]">
                  {{ task.name }}
                </a>
              </div>
              <div class="mt-1.5 flex flex-wrap items-center gap-2">
                <span
                  class="rounded px-1.5 py-0.5 text-xs font-medium"
                  [class]="statusBadgeClass(task.status)">
                  {{ statusLabel(task.status) }}
                </span>
                @if (task.priority !== null && task.priority !== undefined) {
                  <span
                    class="text-xs font-medium"
                    [class]="priorityClass(task.priority)">
                    {{ priorityLabel(task.priority) }}
                  </span>
                }
                <span class="text-muted text-xs">{{ task.projectName }}</span>
              </div>
            </div>

            @if (canManage() && sprint().status !== sprintStatus.completed) {
              <button
                app-stroked-button
                color="primary"
                type="button"
                [disabled]="updateLoading()"
                (click)="onRemoveTask(task.id)">
                Remove
              </button>
            }
          </div>
        } @empty {
          <div class="text-muted p-6 text-center text-sm">
            No tasks in this sprint.
          </div>
        }
      </app-card>
    </div>
  `,
})
export class SprintTaskListComponent {
  private store = inject(Store);

  readonly sprint = input.required<SprintDetailViewModel>();
  readonly canManage = input.required<boolean>();

  readonly sprintStatus = SprintStatus;
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);

  onRemoveTask(taskId?: number) {
    const sprintId = this.sprint().id;
    if (!sprintId || !taskId) return;
    this.store.dispatch(removeTaskFromSprint({ sprintId, taskId }));
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
