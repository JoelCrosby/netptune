import { Component, computed, input } from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';

@Component({
  selector: 'app-task-priority',
  imports: [],
  template: `
    @if (hasPriority()) {
      <span class="text-sm font-medium" [class]="colorClass()">
        {{ label() }}
      </span>
    } @else {
      <span class="text-muted text-sm" i18n="Shown in place of an empty value">
        None
      </span>
    }
  `,
})
export class TaskPriorityComponent {
  readonly priority = input<TaskPriority | null | undefined>(null);

  // TaskPriority.none is 0, so this cannot lean on truthiness.
  readonly hasPriority = computed(() => {
    const priority = this.priority();

    return priority !== null && priority !== undefined;
  });

  readonly label = computed(() => {
    const priority = this.priority();

    if (priority === null || priority === undefined) return '';

    return taskPriorityLabels[priority];
  });

  readonly colorClass = computed(() => {
    const priority = this.priority();

    if (priority === null || priority === undefined) return '';

    return taskPriorityColors[priority];
  });
}
