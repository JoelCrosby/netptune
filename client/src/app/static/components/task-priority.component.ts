import { Component, computed, input } from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';

export type TaskPrioritySize = 'small' | 'medium';

const sizeClasses: Record<TaskPrioritySize, string> = {
  small: 'text-xs',
  medium: 'text-sm',
};

@Component({
  selector: 'app-task-priority',
  imports: [],
  template: `
    @if (hasPriority()) {
      <span class="font-medium" [class]="sizeClass() + ' ' + colorClass()">
        {{ label() }}
      </span>
    } @else {
      <span
        class="text-muted"
        [class]="sizeClass()"
        i18n="Shown in place of an empty value">
        None
      </span>
    }
  `,
})
export class TaskPriorityComponent {
  readonly priority = input<TaskPriority | null | undefined>(null);
  readonly size = input<TaskPrioritySize>('medium');

  readonly sizeClass = computed(() => sizeClasses[this.size()]);

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
