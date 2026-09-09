import { Component, input } from '@angular/core';

@Component({
  selector: 'app-automation-flow-card',
  host: {
    class: 'border-border bg-card block rounded-lg border shadow-sm',
  },
  template: `
    <div class="flex min-h-11 items-center gap-2.5 px-3.5 pt-2.5 pb-0">
      <p
        class="text-primary shrink-0 text-[0.6875rem] font-bold tracking-[0.1em]">
        {{ keyword() }}
      </p>

      @if (heading(); as headingText) {
        <p class="truncate text-[0.9375rem] font-semibold">{{ headingText }}</p>
      }

      <div class="flex min-w-0 flex-1 items-center gap-2.5">
        <ng-content select="[flowCardHeader]" />
      </div>

      <div class="flex shrink-0 items-center gap-1">
        <ng-content select="[flowCardActions]" />
      </div>
    </div>

    <div class="flex min-w-0 flex-col gap-3.5 p-3.5">
      <ng-content />
    </div>
  `,
})
export class AutomationFlowCardComponent {
  readonly keyword = input.required<string>();
  readonly heading = input<string | null>(null);
}
