import { Component, computed, input, model } from '@angular/core';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { FieldRowComponent } from '@static/components/field-row/field-row.component';
import { TaskFieldBase } from './field-label';

export type TaskDateFieldKind = 'start' | 'due';

@Component({
  selector: 'app-task-date-field',
  imports: [DatePickerComponent, FieldRowComponent],
  host: { class: 'block' },
  template: `
    <div
      app-field-row
      mode="container"
      [label]="copy().label"
      [labelWidth]="labelWidth()">
      <app-date-picker
        class="min-w-0 flex-1"
        appearance="bare"
        [buttonClass]="dateButtonClass"
        [showLeadingIcon]="false"
        [showChevron]="false"
        [disabled]="disabled()"
        i18n-placeholder="Shown in place of a date the task does not have"
        placeholder="Not set"
        [ariaLabel]="copy().ariaLabel"
        [(value)]="value" />
    </div>
  `,
})
export class TaskDateFieldComponent extends TaskFieldBase {
  readonly kind = input.required<TaskDateFieldKind>();
  readonly value = model('');

  protected readonly dateButtonClass =
    'h-8 w-auto gap-2 px-0 text-[13px] font-medium';

  protected readonly copy = computed(() => {
    if (this.kind() === 'start') {
      return {
        label: $localize`:Field heading for the task start date:Start date`,
        ariaLabel: $localize`:Accessible label for the task start date picker:Start date`,
      };
    }

    return {
      label: $localize`:Field heading for the task due date:Due date`,
      ariaLabel: $localize`:Accessible label for the task due date picker:Due date`,
    };
  });
}
