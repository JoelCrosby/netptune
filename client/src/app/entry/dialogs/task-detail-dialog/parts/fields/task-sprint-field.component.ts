import { Component, input, model } from '@angular/core';
import { fieldRowClass } from '@static/components/field-row/field-row.component';
import { TaskSprintPickerComponent } from '../../pickers/task-sprint-picker.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-sprint-field',
  imports: [TaskSprintPickerComponent],
  host: { class: 'block' },
  template: `
    <app-task-sprint-picker
      [buttonClass]="rowClass"
      [disabled]="disabled()"
      [projectId]="projectId()"
      [(value)]="value">
      <span [class]="labelClass()">{{ labels.sprint }}</span>
      @if (sprintName(); as name) {
        <span class="truncate font-medium">{{ name }}</span>
      } @else {
        <span class="text-muted">{{ labels.noSprint }}</span>
      }
    </app-task-sprint-picker>
  `,
})
export class TaskSprintFieldComponent extends TaskFieldBase {
  readonly value = model<number | null>(null);
  readonly projectId = input<number | null>(null);
  readonly sprintName = input<string | null | undefined>(null);

  protected readonly rowClass = fieldRowClass;

  protected readonly labels = {
    sprint: $localize`:Field heading for the task's sprint:Sprint`,
    noSprint: $localize`:Shown in place of a sprint name when a task has no sprint:No Sprint`,
  };
}
