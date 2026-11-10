import { Component, input } from '@angular/core';

export interface FieldTableRow {
  readonly label: string;
  readonly value: string;
  readonly note?: string;
  readonly tag?: string;
}

// A bordered two-column table of field names and the values to enter, under a
// heading with room for an action on the right.
@Component({
  selector: 'app-field-table',
  host: { class: 'block' },
  template: `
    <div class="border-border overflow-hidden rounded-md border">
      <div
        class="bg-foreground/4 border-border flex flex-wrap items-center justify-between gap-3 border-b px-3 py-2">
        <span class="text-muted text-xs font-semibold">{{ heading() }}</span>
        <ng-content select="[fieldTableAction]" />
      </div>
      <dl
        class="divide-border m-0 grid grid-cols-[max-content_1fr] text-[13px] [&>*]:border-b [&>*]:px-3 [&>*]:py-1.75 [&>:nth-last-child(-n+2)]:border-b-0">
        @for (row of rows(); track row.label) {
          <dt class="text-muted border-border">{{ row.label }}</dt>
          <dd class="border-border m-0 font-medium">
            {{ row.value }}
            @if (row.note) {
              <span class="text-muted font-normal">{{ row.note }}</span>
            }
            @if (row.tag) {
              <span
                class="bg-foreground/8 text-muted ml-1.5 inline-block rounded px-1.5 py-px align-[1px] text-[11px] font-semibold">
                {{ row.tag }}
              </span>
            }
          </dd>
        }
      </dl>
    </div>
  `,
})
export class FieldTableComponent {
  readonly heading = input.required<string>();
  readonly rows = input.required<readonly FieldTableRow[]>();
}
