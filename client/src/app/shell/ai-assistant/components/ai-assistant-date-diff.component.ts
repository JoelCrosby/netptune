import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';
import { ValueSwapComponent } from '@static/components/value-swap/value-swap.component';
import { dateAfter, dateBefore, dayShift } from './ai-assistant-field-diff';

@Component({
  selector: 'app-ai-assistant-date-diff',
  host: { class: 'block' },
  imports: [DatePipe, ValueChipComponent, ValueSwapComponent],
  template: `
    <app-value-swap [hasBefore]="!!before()">
      @if (before(); as before) {
        <app-value-chip
          before
          tone="removed"
          [label]="(before | date: 'mediumDate') ?? ''" />
      }
      @if (after(); as after) {
        <app-value-chip
          after
          tone="added"
          [label]="(after | date: 'mediumDate') ?? ''" />
      } @else {
        <app-value-chip
          after
          tone="empty"
          i18n-label="Shown in place of a date a change removes"
          label="No date" />
      }
      @if (shiftLabel(); as shift) {
        <span class="text-muted text-[13px]">{{ shift }}</span>
      }
    </app-value-swap>
  `,
})
export class AiAssistantDateDiffComponent {
  readonly field = input.required<AiChangeField>();

  protected readonly before = computed(() => dateBefore(this.field()));
  protected readonly after = computed(() => dateAfter(this.field()));

  protected readonly shiftLabel = computed(() => {
    const days = dayShift(this.before(), this.after());

    if (days === 0) {
      return null;
    }

    const count = Math.abs(days);
    const isOneDay = count === 1;

    if (days > 0 && isOneDay) {
      return $localize`:How far a change moves a date forward:1 day later`;
    }

    if (days > 0) {
      return $localize`:How far a change moves a date forward:${count}:count: days later`;
    }

    if (isOneDay) {
      return $localize`:How far a change moves a date back:1 day earlier`;
    }

    return $localize`:How far a change moves a date back:${count}:count: days earlier`;
  });
}
