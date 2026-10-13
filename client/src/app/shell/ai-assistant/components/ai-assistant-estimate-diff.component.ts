import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';
import { ValueSwapComponent } from '@static/components/value-swap/value-swap.component';
import { formatChangeEstimate } from './ai-assistant-field-diff';

@Component({
  selector: 'app-ai-assistant-estimate-diff',
  host: { class: 'block' },
  imports: [ValueChipComponent, ValueSwapComponent],
  template: `
    <app-value-swap [hasBefore]="!!before()">
      @if (before(); as before) {
        <app-value-chip before tone="removed" mono [label]="before" />
      }
      @if (after(); as after) {
        <app-value-chip after tone="added" mono [label]="after" />
      } @else {
        <app-value-chip
          after
          tone="empty"
          i18n-label="Shown in place of an estimate a change removes"
          label="No estimate" />
      }
    </app-value-swap>
  `,
})
export class AiAssistantEstimateDiffComponent {
  readonly field = input.required<AiChangeField>();

  protected readonly before = computed(() => {
    return formatChangeEstimate(this.field().before);
  });

  protected readonly after = computed(() => {
    return formatChangeEstimate(this.field().after);
  });
}
