import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';
import { ValueSwapComponent } from '@static/components/value-swap/value-swap.component';
import { choiceAfter, choiceBefore } from './ai-assistant-field-diff';

@Component({
  selector: 'app-ai-assistant-choice-diff',
  host: { class: 'block' },
  imports: [ValueChipComponent, ValueSwapComponent],
  template: `
    <app-value-swap [hasBefore]="!!before()">
      @if (before(); as before) {
        <app-value-chip
          before
          tone="removed"
          [label]="before.label"
          [color]="before.color"
          [accent]="before.accent" />
      }
      @if (after(); as after) {
        <app-value-chip
          after
          tone="added"
          [label]="after.label"
          [color]="after.color"
          [accent]="after.accent" />
      } @else {
        <app-value-chip
          after
          tone="empty"
          i18n-label="Shown in place of a value a change clears"
          label="Cleared" />
      }
    </app-value-swap>
  `,
})
export class AiAssistantChoiceDiffComponent {
  readonly field = input.required<AiChangeField>();

  protected readonly before = computed(() => choiceBefore(this.field()));
  protected readonly after = computed(() => choiceAfter(this.field()));
}
