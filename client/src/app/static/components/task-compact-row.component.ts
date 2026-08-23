import { Component, computed, input } from '@angular/core';
import { formatEstimate } from '@core/enums/estimate-type';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskAssigneesComponent } from './task-assignees.component';
import { TaskPriorityComponent } from './task-priority.component';
import { TaskStatusPillComponent } from './task-status-pill.component';

@Component({
  selector: 'app-task-compact-row',
  imports: [
    TaskAssigneesComponent,
    TaskPriorityComponent,
    TaskStatusPillComponent,
  ],
  host: {
    class:
      'grid grid-cols-[64px_1fr_auto_auto_auto] items-center gap-3 px-4 py-3',
  },
  template: `
    <span class="font-avatar text-muted text-xs">
      {{ task().systemId }}
    </span>

    <span class="min-w-0 truncate text-sm">
      {{ task().name }}
      @if (isOverdue()) {
        <span class="text-xs text-orange-600 dark:text-orange-300">
          &nbsp;·&nbsp;<ng-container
            i18n="Marks a task whose due date has passed"
            >overdue</ng-container
          >
        </span>
      }
    </span>

    @if (task().priority !== null) {
      <app-task-priority size="small" [priority]="task().priority" />
    } @else {
      <span></span>
    }

    <span class="font-avatar text-muted text-xs tabular-nums">
      {{ estimateLabel() }}
    </span>

    <span class="flex items-center gap-2">
      <app-task-status-pill
        [name]="task().statusName"
        [color]="task().statusColor"
        [category]="task().statusCategory" />
      <app-task-assignees [assignees]="task().assignees" />
    </span>
  `,
})
export class TaskCompactRowComponent {
  readonly task = input.required<TaskViewModel>();

  protected readonly estimateLabel = computed(() => {
    const { estimateType, estimateValue } = this.task();

    if (estimateType === null || estimateValue === null) return '';

    return formatEstimate(estimateType, estimateValue);
  });

  protected readonly isOverdue = computed(() => {
    const dueDate = this.task().dueDate;

    if (!dueDate) return false;

    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);

    return new Date(dueDate).getTime() < startOfToday.getTime();
  });
}
