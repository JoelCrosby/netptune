import { Component, computed, input } from '@angular/core';
import { LucideArrowRight } from '@lucide/angular';

export type ValueSwapDensity = 'default' | 'inline';

const densityClasses: Record<ValueSwapDensity, string> = {
  default: 'gap-2.5 px-4 py-3.5',
  inline: 'gap-2',
};

@Component({
  selector: 'app-value-swap',
  imports: [LucideArrowRight],
  host: { '[class]': 'hostClass()' },
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
  readonly density = input<ValueSwapDensity>('default');
  readonly separatorLabel = input(
    $localize`:Read by screen readers between an old value and the value replacing it:changed to`
  );

  protected readonly hostClass = computed(() => {
    return `flex flex-wrap items-center ${densityClasses[this.density()]}`;
  });
}
