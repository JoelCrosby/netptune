import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

export type ListGroupHeaderAppearance = 'bar' | 'plain';

@Component({
  selector: 'app-list-group-header',
  host: { '[class]': 'hostClass()' },
  template: `
    <span class="flex-1">
      {{ label() }}
      @if (count() !== null) {
        <span>·&nbsp;{{ count() }}</span>
      }
    </span>
    <ng-content />
  `,
})
export class ListGroupHeaderComponent {
  readonly label = input.required<string>();
  readonly count = input<number | null>(null);
  readonly appearance = input<ListGroupHeaderAppearance>('bar');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    const appearanceClass =
      this.appearance() === 'plain'
        ? 'text-foreground/45 px-2 pt-3.5 pb-1 text-[11px] tracking-[0.06em]'
        : 'bg-card-header border-border text-muted sticky top-0 z-10 border-b px-4 py-2 text-xs tracking-wide';

    return cn(
      'flex items-center gap-2 font-semibold uppercase',
      appearanceClass,
      this.class()
    );
  });
}
