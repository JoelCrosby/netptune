import { Component, computed, input } from '@angular/core';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import { signalToneText } from './tones';

@Component({
  selector: 'app-task-estimate',
  host: {
    class: 'font-mono text-xs',
    '[class]': 'toneClass()',
  },
  template: `{{ label() }}`,
})
export class TaskEstimateComponent {
  readonly estimateType = input<EstimateType | null | undefined>(null);
  readonly estimateValue = input<number | null | undefined>(null);

  private readonly isMissing = computed(() => {
    return this.estimateType() == null || this.estimateValue() == null;
  });

  protected readonly label = computed(() => {
    const type = this.estimateType();
    const value = this.estimateValue();

    if (type == null || value == null) return '—';

    return formatEstimate(type, value);
  });

  protected readonly toneClass = computed(() => {
    return this.isMissing() ? signalToneText.warn : 'text-foreground/60';
  });
}
