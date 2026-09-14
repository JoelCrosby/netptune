import { Component, model } from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { LucideFlag } from '@lucide/angular';
import { fieldRowClass } from '@static/components/field-row/field-row.component';
import { TaskPriorityPickerComponent } from '../../pickers/task-priority-picker.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-priority-field',
  imports: [LucideFlag, TaskPriorityPickerComponent],
  host: { class: 'block' },
  template: `
    <app-task-priority-picker
      [buttonClass]="rowClass"
      [disabled]="disabled()"
      [(value)]="value">
      <span [class]="labelClass()">{{ labels.priority }}</span>
      @let priority = value();
      @if (priority === null) {
        <span class="text-muted">{{ labels.notSet }}</span>
      } @else {
        <span
          class="flex items-center gap-2 font-medium"
          [class]="priorityColors[priority]">
          <svg lucideFlag class="h-3.5 w-3.5"></svg>
          {{ priorityLabels[priority] }}
        </span>
      }
    </app-task-priority-picker>
  `,
})
export class TaskPriorityFieldComponent extends TaskFieldBase {
  readonly value = model<TaskPriority | null>(null);

  protected readonly rowClass = fieldRowClass;
  protected readonly priorityColors = taskPriorityColors;
  protected readonly priorityLabels = taskPriorityLabels;

  protected readonly labels = {
    priority: $localize`:Field heading for the task priority:Priority`,
    notSet: $localize`:Shown in place of a value the task does not have:Not set`,
  };
}
