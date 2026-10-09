import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';

// Stays hidden until the text gets close to the limit, so it only interrupts when it matters.
const visibleFromRatio = 0.9;

@Component({
  selector: 'app-character-limit',
  imports: [DecimalPipe],
  template: `
    @if (isVisible()) {
      <div
        class="text-xs font-medium tabular-nums"
        [class.text-warn]="isOver()"
        [class.text-foreground/50]="!isOver()"
        aria-live="polite">
        @if (isOver()) {
          <span i18n="Shown under a text field when the text is longer than allowed">
            {{ overBy() | number // i18n(ph="OVER_BY") }} characters over the
            {{ max() | number // i18n(ph="MAX_LENGTH") }} character limit
          </span>
        } @else {
          {{ length() | number }} / {{ max() | number }}
        }
      </div>
    }
  `,
})
export class CharacterLimitComponent {
  readonly length = input.required<number>();
  readonly max = input.required<number>();

  readonly isOver = computed(() => this.length() > this.max());
  readonly overBy = computed(() => this.length() - this.max());
  readonly isVisible = computed(
    () => this.length() >= this.max() * visibleFromRatio
  );
}
