import { Component, input } from '@angular/core';

export type StatValue = number | string;

@Component({
  selector: 'app-stat',
  template: `
    <div
      class="border-border bg-card-header flex min-h-24 flex-col items-start justify-center rounded border p-6 shadow-sm">
      <span class="text-muted text-xs font-medium uppercase">
        {{ label() }}
      </span>
      <strong class="mt-1 text-2xl">{{ value() }}</strong>
    </div>
  `,
})
export class StatComponent {
  readonly label = input.required<string>();
  readonly value = input.required<StatValue>();
}
