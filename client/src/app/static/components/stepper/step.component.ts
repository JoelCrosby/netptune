import { Component, input, signal } from '@angular/core';

@Component({
  selector: 'app-step',
  template: `
    <div class="flex gap-4">
      <div class="flex flex-col items-center">
        <div
          class="border-border bg-background text-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full border text-sm font-medium">
          {{ index() }}
        </div>
        @if (!last()) {
          <div class="bg-border mt-1 w-px grow"></div>
        }
      </div>

      <div class="min-w-0 flex-1" [class.pb-14]="!last()">
        <div class="flex min-h-8 flex-col justify-center">
          <h3 class="text-foreground text-sm font-semibold leading-none">
            {{ title() }}
          </h3>
          @if (description()) {
            <p class="text-muted mt-1 text-sm">{{ description() }}</p>
          }
        </div>

        <div class="mt-4">
          <ng-content />
        </div>
      </div>
    </div>
  `,
})
export class StepComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();

  /** Populated by the parent `app-stepper`. */
  readonly index = signal(1);
  readonly last = signal(false);

  setPosition(index: number, last: boolean) {
    this.index.set(index);
    this.last.set(last);
  }
}
