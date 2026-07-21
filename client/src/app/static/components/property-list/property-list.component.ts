import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';

export type PropertyListValue = string | number | Date | null | undefined;

export interface PropertyListItem {
  readonly label: string;
  readonly value: PropertyListValue;
  readonly format?: 'date';
  readonly monospace?: boolean;
  readonly breakAll?: boolean;
}

@Component({
  selector: 'app-property-list',
  imports: [DatePipe],
  host: { class: 'block' },
  template: `
    <section>
      <h2 class="text-muted mb-2 text-xs font-medium uppercase">
        {{ heading() }}
      </h2>
      <dl class="grid grid-cols-[auto_1fr] gap-x-5 gap-y-2 text-sm">
        @for (item of items(); track item.label) {
          <dt class="text-muted">{{ item.label }}</dt>
          <dd
            [class.font-mono]="item.monospace"
            [class.text-xs]="item.monospace"
            [class.break-all]="item.breakAll">
            @if (hasValue(item.value)) {
              @switch (item.format) {
                @case ('date') {
                  {{ item.value | date: 'medium' }}
                }
                @default {
                  {{ item.value }}
                }
              }
            } @else {
              —
            }
          </dd>
        }
      </dl>
    </section>
  `,
})
export class PropertyListComponent {
  readonly heading = input.required<string>();
  readonly items = input.required<readonly PropertyListItem[]>();

  protected hasValue(value: PropertyListValue): boolean {
    return value !== null && value !== undefined && value !== '';
  }
}
