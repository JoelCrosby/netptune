import { Component, input, signal } from '@angular/core';

@Component({
  selector: 'app-step',
  host: {
    class: 'col-start-1 row-start-1 block w-full min-w-0',
    '[class.invisible]': 'wizard() && !active()',
    '[class.pointer-events-none]': 'wizard() && !active()',
    '[class.z-10]': 'wizard() && active()',
    '[class.z-0]': 'wizard() && !active()',
    '[attr.aria-hidden]': "wizard() && !active() ? 'true' : null",
  },
  template: `
    <div class="h-full min-w-0" [class.flex]="!wizard()">
      @if (!wizard()) {
        <div class="mr-4 flex flex-col items-center">
          <div
            class="border-border bg-background text-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full border text-sm font-medium">
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
