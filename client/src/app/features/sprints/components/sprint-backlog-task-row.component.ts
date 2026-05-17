import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskStatus, taskStatusLabels } from '@core/enums/project-task-status';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { assignBacklogTask } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';

@Component({
  selector: 'app-sprint-backlog-task-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'border-border flex items-center justify-between gap-4 border-b p-4 last:border-b-0',
  },
  imports: [RouterLink, FlatButtonComponent, TaskScopeIdComponent],
  template: `
    <div class="min-w-0 flex-1">
      <div class="flex flex-wrap items-center gap-2">
        <app-task-scope-id [id]="task().systemId" />
        <a
          class="truncate font-medium"
          [routerLink]="['../../tasks', task().systemId]">
          {{ task().name }}
        </a>
      </div>
      <div class="mt-1.5 flex flex-wrap items-center gap-2">
        <span
          class="rounded px-1.5 py-0.5 text-xs font-medium"
          [class]="statusBadgeClass(task().status)">
          {{ statusLabel(task().status) }}
        </span>
        @if (task().priority !== null && task().priority !== undefined) {
          <span
            class="text-xs font-medium"
            [class]="priorityClass(task().priority!)">
            {{ priorityLabel(task().priority!) }}
          </span>
        }
        <span class="text-muted text-xs">{{ task().projectName }}</span>
      </div>
    </div>

    @if (sprints().length > 0) {
      <div class="flex shrink-0 items-center gap-2">
        <select
          class="border-border bg-background rounded border px-2 py-1.5 text-sm"
          [value]="selectedSprintId() ?? ''"
          (change)="onSprintSelected($event)">
          <option value="" disabled>Assign to sprint…</option>
          @for (sprint of sprints(); track sprint.id) {
            <option [value]="sprint.id">{{ sprint.name }}</option>
          }
        </select>
        <button
          app-flat-button
          color="primary"
          type="button"
          [disabled]="loading() || !selectedSprintId()"
          (click)="onAssign()">
          Add
        </button>
      </div>
    }
  `,
})
export class SprintBacklogTaskRowComponent {
  private store = inject(Store);

  readonly task = input.required<TaskViewModel>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly loading = this.store.selectSignal(selectSprintUpdateLoading);

  readonly selectedSprintId = signal<number | undefined>(undefined);

  onSprintSelected(event: Event) {
    const sprintId = Number((event.target as HTMLSelectElement).value);
    this.selectedSprintId.set(sprintId);
  }

  onAssign() {
    const taskId = this.task().id;
    const sprintId = this.selectedSprintId();
    if (!taskId || !sprintId) return;

    this.store.dispatch(assignBacklogTask({ taskId, sprintId }));
    this.selectedSprintId.set(undefined);
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
