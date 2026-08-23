import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

export interface StatStripItem {
  label: string;
  value: string | number;
  suffix?: string;
  valueClass?: string;
}

export type StatStripDensity = 'comfortable' | 'compact';

const columnClasses: Record<number, string> = {
  1: 'sm:grid-cols-1',
  2: 'sm:grid-cols-2 sm:divide-x sm:divide-y-0',
  3: 'sm:grid-cols-3 sm:divide-x sm:divide-y-0',
  4: 'sm:grid-cols-4 sm:divide-x sm:divide-y-0',
};

const cellClasses: Record<StatStripDensity, string> = {
  comfortable: 'px-6 py-4',
  compact: 'px-4 py-2.5',
};

const labelClasses: Record<StatStripDensity, string> = {
  comfortable: 'text-muted text-xs font-medium tracking-wide uppercase',
  compact: 'text-muted text-[11px] font-medium tracking-[0.04em] uppercase',
};

@Component({
  selector: 'app-stat-strip',
  host: { class: 'block' },
  template: `
    <dl [class]="stripClass()">
      @for (item of items(); track item.label) {
        <div [class]="cellClass()">
          <dt [class]="labelClass()">{{ item.label }}</dt>
          <dd [class]="valueClass(item)">
            {{ item.value }}
            @if (item.suffix) {
              <span class="text-muted text-sm font-normal">
                {{ item.suffix }}
              </span>
            }
          </dd>
        </div>
      }
    </dl>
  `,
})
export class StatStripComponent {
  readonly items = input.required<readonly StatStripItem[]>();
  readonly density = input<StatStripDensity>('comfortable');

  protected readonly stripClass = computed(() => {
    const columns = columnClasses[this.items().length] ?? columnClasses[3];

    return cn(
      'border-border divide-border grid grid-cols-1 divide-y border-t',
      columns
    );
  });

  protected readonly cellClass = computed(() => cellClasses[this.density()]);

  protected readonly labelClass = computed(() => labelClasses[this.density()]);

  protected valueClass(item: StatStripItem): string {
    const spacing = this.density() === 'compact' ? 'mt-0.5' : 'mt-1';

    return cn(
      'text-lg font-semibold tabular-nums',
      spacing,
      item.valueClass ?? ''
    );
  }
}
