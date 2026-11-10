import { Component, input } from '@angular/core';

export type NumberedStepState = 'done' | 'current' | 'upcoming';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'ol[app-numbered-steps]',
  host: { class: 'm-0 flex list-none flex-col p-0' },
  template: '<ng-content />',
})
export class NumberedStepsComponent {}

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'li[app-numbered-step]',
  host: {
    class: 'group relative grid grid-cols-[1.75rem_1fr] gap-x-3 pb-5 last:pb-0',
    '[attr.aria-current]': "state() === 'current' ? 'step' : null",
  },
  template: `
    <span
      class="bg-border absolute top-7.5 bottom-1 left-3.25 w-px group-last:hidden"
      aria-hidden="true"></span>
    <span
      class="flex h-7 w-7 items-center justify-center rounded-full border text-xs font-semibold tabular-nums"
      [class.bg-primary]="state() === 'done'"
      [class.border-primary]="state() !== 'upcoming'"
      [class.text-primary-foreground]="state() === 'done'"
      [class.text-primary]="state() === 'current'"
      [class.border-border]="state() === 'upcoming'"
      [class.text-muted]="state() === 'upcoming'"
      aria-hidden="true">
      {{ number() }}
    </span>
    <div class="flex min-w-0 flex-col gap-2 pt-0.75">
      <span class="font-semibold">{{ title() }}</span>
      <ng-content />
    </div>
  `,
})
export class NumberedStepComponent {
  readonly number = input.required<number>();
  readonly title = input.required<string>();
  readonly state = input<NumberedStepState>('upcoming');
}
