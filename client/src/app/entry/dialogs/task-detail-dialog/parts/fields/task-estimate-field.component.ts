import { Component, computed, input, output } from '@angular/core';
import {
  EstimateType,
  formatEstimate,
  TaskEstimate,
} from '@core/enums/estimate-type';
import { fieldRowClass } from '@static/components/field-row/field-row.component';
import { TaskEstimatePickerComponent } from '../../pickers/task-estimate-picker.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-estimate-field',
  imports: [TaskEstimatePickerComponent],
  host: { class: 'block' },
  template: `
    <app-task-estimate-picker
      [buttonClass]="rowClass"
      [disabled]="disabled()"
      [estimateType]="estimateType()"
      [estimateValue]="estimateValue()"
      (estimateChange)="estimateChange.emit($event)">
      <span [class]="labelClass()">{{ labels.estimate }}</span>
      @if (estimateLabel(); as estimate) {
        <span class="font-medium">{{ estimate }}</span>
      } @else {
        <span class="text-muted">{{ labels.notSet }}</span>
      }
    </app-task-estimate-picker>
  `,
})
export class TaskEstimateFieldComponent extends TaskFieldBase {
  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);

  readonly estimateChange = output<TaskEstimate>();

  protected readonly rowClass = fieldRowClass;

  protected readonly labels = {
    estimate: $localize`:Field heading for the task effort estimate:Estimate`,
    notSet: $localize`:Shown in place of a value the task does not have:Not set`,
  };

  protected readonly estimateLabel = computed(() => {
    const value = this.estimateValue();

    if (value === null) return '';

    return formatEstimate(
      this.estimateType() ?? EstimateType.storyPoints,
      value
    );
  });
}
