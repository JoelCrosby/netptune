import { Component, input } from '@angular/core';
import { LucideArrowRight } from '@lucide/angular';

@Component({
  selector: 'app-value-swap',
  imports: [LucideArrowRight],
  host: { class: 'flex flex-wrap items-center gap-2.5 px-4 py-3.5' },
  template: `
    @if (hasBefore()) {
      <ng-content select="[before]" />
      <svg
        lucideArrowRight
        class="text-muted h-4 w-4 shrink-0"
        aria-hidden="true"></svg>
      <span class="sr-only">{{ separatorLabel() }}</span>
    }
    <ng-content select="[after]" />
    <ng-content />
  `,
})
export class ValueSwapComponent {
  readonly hasBefore = input(true);
  readonly separatorLabel = input(
    $localize`:Read by screen readers between an old value and the value replacing it:changed to`
  );
}
