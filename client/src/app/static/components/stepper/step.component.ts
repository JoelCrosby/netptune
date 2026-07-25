import { Component, input, signal } from '@angular/core';

@Component({
  selector: 'app-step',
  host: {
    class: 'col-start-1 row-start-1 w-full min-w-0',
    '[class.block]': '!wizard() || active()',
    '[class.hidden]': 'wizard() && !active()',
    '[attr.aria-hidden]': "wizard() && !active() ? 'true' : null",
  },
  template: `
    <div class="h-full min-w-0" [class.flex]="!wizard()">
      @if (!wizard()) {
        <div class="mr-4 flex flex-col items-center">
          <div
            class="bg-background flex h-8 w-8 shrink-0 items-center justify-center rounded-full border text-sm font-medium"
            [class.border-border]="!error()"
            [class.text-foreground]="!error()"
            [class.border-warn]="error()"
            [class.text-warn]="error()">
            {{ index() }}
          </div>
          @if (!last()) {
            <div class="bg-border mt-1 w-px grow"></div>
          }
        </div>
      }

      <div class="min-w-0 flex-1" [class.pb-14]="!wizard() && !last()">
        <div class="mt-4">
          <ng-content />
        </div>

        @if (error()) {
          <p class="text-warn mt-3 text-sm" role="alert">{{ error() }}</p>
        }
      </div>
    </div>
  `,
})
export class StepComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
  readonly error = input<string | null>(null);

  /** Populated by the parent `app-stepper`. */
  readonly index = signal(1);
  readonly last = signal(false);
  readonly wizard = signal(false);
  readonly active = signal(true);

  setState(state: StepState) {
    this.index.set(state.index);
    this.last.set(state.last);
    this.wizard.set(state.wizard);
    this.active.set(state.active);
  }
}

interface StepState {
  index: number;
  last: boolean;
  wizard: boolean;
  active: boolean;
}
