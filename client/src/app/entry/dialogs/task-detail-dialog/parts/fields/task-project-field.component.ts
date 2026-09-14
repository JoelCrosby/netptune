import { Component, input, model } from '@angular/core';
import { fieldRowClass } from '@static/components/field-row/field-row.component';
import { TaskProjectPickerComponent } from '../../pickers/task-project-picker.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-project-field',
  imports: [TaskProjectPickerComponent],
  host: { class: 'block' },
  template: `
    <app-task-project-picker
      [buttonClass]="rowClass"
      [disabled]="disabled()"
      [(value)]="value">
      <span [class]="labelClass()">{{ labels.project }}</span>
      @if (projectName(); as name) {
        <span class="truncate font-medium">{{ name }}</span>
      } @else {
        <span class="text-muted">{{ labels.chooseProject }}</span>
      }
    </app-task-project-picker>
  `,
})
export class TaskProjectFieldComponent extends TaskFieldBase {
  readonly value = model<number | null>(null);
  readonly projectName = input<string | null | undefined>(null);

  protected readonly rowClass = fieldRowClass;

  protected readonly labels = {
    project: $localize`:Field heading for the task's project:Project`,
    chooseProject: $localize`:Shown in the project row of the create-task dialog before a project is picked:Choose a project`,
  };
}
