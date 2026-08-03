import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

export interface StatStripItem {
  label: string;
  value: string | number;
}

const columnClasses: Record<number, string> = {
  1: 'sm:grid-cols-1',
  2: 'sm:grid-cols-2 sm:divide-x sm:divide-y-0',
  3: 'sm:grid-cols-3 sm:divide-x sm:divide-y-0',
  4: 'sm:grid-cols-4 sm:divide-x sm:divide-y-0',
};

@Component({
  selector: 'app-stat-strip',
  host: { class: 'block' },
  template: `
    <dl [class]="stripClass()">
      @for (item of items(); track item.label) {
        <div class="px-6 py-4">
          <dt class="text-muted text-xs font-medium tracking-wide uppercase">
            {{ item.label }}
          </dt>
          <dd class="mt-1 text-lg font-semibold tabular-nums">
            {{ item.value }}
          </dd>
        </div>
      }
    </dl>
  `,
})
export class StatStripComponent {
  readonly items = input.required<readonly StatStripItem[]>();
  readonly class = input('');

  protected readonly stripClass = computed(() => {
    const columns = columnClasses[this.items().length] ?? columnClasses[3];

    return cn(
      'border-border divide-border grid grid-cols-1 divide-y border-t',
      columns,
      this.class()
    );
  });
}
