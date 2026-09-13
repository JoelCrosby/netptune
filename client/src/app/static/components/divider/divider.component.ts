import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

export type DividerOrientation = 'horizontal' | 'vertical';

@Component({
  selector: 'app-divider',
  host: {
    role: 'separator',
    '[class]': 'hostClass()',
    '[attr.aria-orientation]': 'ariaOrientation()',
  },
  template: `
    @if (orientation() === 'horizontal') {
      @if (label(); as label) {
        <div class="flex items-center gap-3">
          <span class="bg-foreground/7 h-px grow"></span>
          <span
            class="text-foreground/45 text-[11px] font-bold tracking-[.1em] uppercase">
            {{ label }}
          </span>
          <span class="bg-foreground/7 h-px grow"></span>
        </div>
      } @else {
        <span class="bg-foreground/7 block h-px w-full"></span>
      }
    }
  `,
})
export class DividerComponent {
  readonly label = input<string | null>(null);
  readonly orientation = input<DividerOrientation>('horizontal');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    const base =
      this.orientation() === 'vertical'
        ? 'bg-border block h-6 w-px shrink-0'
        : 'block';

    return cn(base, this.class());
  });

  protected readonly ariaOrientation = computed(() => {
    return this.orientation() === 'vertical' ? 'vertical' : null;
  });
}
