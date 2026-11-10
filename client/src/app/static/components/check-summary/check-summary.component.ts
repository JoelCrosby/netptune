import { Component, input } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'ul[app-check-summary]',
  host: { class: 'm-0 flex list-none flex-col gap-1.5 p-0 text-[13px]' },
  template: '<ng-content />',
})
export class CheckSummaryComponent {}

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'li[app-check-summary-row]',
  imports: [LucideCheck],
  host: { class: 'text-muted flex min-w-0 items-center gap-2.5' },
  template: `
    <svg
      lucideCheck
      class="h-4 w-4 shrink-0 text-emerald-600 dark:text-emerald-400"
      aria-hidden="true"></svg>
    <span class="min-w-0">
      {{ label() }}
      <b class="text-foreground font-medium" [class.font-mono]="mono()">{{
        value()
      }}</b>
      @if (detail(); as detail) {
        <span aria-hidden="true">·</span>
        {{ detail }}
      }
    </span>
    <span class="ml-auto shrink-0 empty:hidden"><ng-content /></span>
  `,
})
export class CheckSummaryRowComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly detail = input<string | null>(null);
  readonly mono = input(false);
}
