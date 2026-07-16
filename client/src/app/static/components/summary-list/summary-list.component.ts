import { Component, input } from '@angular/core';

export interface SummaryListItem {
  readonly label: string;
  readonly value: string;
  readonly muted?: boolean;
  readonly truncate?: boolean;
}

@Component({
  selector: 'app-summary-list',
  host: { class: 'block' },
  template: `
    <dl class="border-border divide-border m-0 divide-y border-t text-sm">
      @for (item of items(); track item.label) {
        <div class="grid grid-cols-3 items-start gap-4 px-5 py-3">
          <dt class="text-muted">{{ item.label }}</dt>
          <dd
            class="col-span-2 m-0 min-w-0 text-right"
            [class.font-medium]="!item.muted"
            [class.text-muted]="item.muted"
            [class.truncate]="item.truncate">
            {{ item.value }}
          </dd>
        </div>
      }
    </dl>
  `,
})
export class SummaryListComponent {
  readonly items = input.required<readonly SummaryListItem[]>();
}
