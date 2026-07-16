import {
  Component,
  contentChildren,
  effect,
  input,
  model,
} from '@angular/core';
import { StepComponent } from './step.component';

@Component({
  selector: 'app-stepper',
  template: `
    @if (mode() === 'wizard') {
      <ol class="m-0 flex list-none items-start p-0" aria-label="Form progress">
        @for (step of steps(); track step; let index = $index, last = $last) {
          <li class="flex min-w-0 items-center" [class.flex-1]="!last">
            <div class="flex min-w-0 flex-col items-center gap-2 text-center">
              <span
                class="border-border bg-background flex h-8 w-8 items-center justify-center rounded-full border text-sm font-medium transition-colors"
                [class.border-primary]="index <= activeIndex()"
                [class.bg-primary]="index < activeIndex()"
                [class.text-background]="index < activeIndex()"
                [class.text-primary]="index === activeIndex()"
                [attr.aria-current]="
                  index === activeIndex() ? 'step' : undefined
                ">
                {{ index + 1 }}
              </span>
              <span
                class="text-muted max-w-28 truncate text-xs"
                [class.text-foreground]="index === activeIndex()">
                {{ step.title() }}
              </span>
            </div>

            @if (!last) {
              <span
                class="bg-border mx-3 mb-6 h-px min-w-4 flex-1"
                [class.bg-primary]="index < activeIndex()"></span>
            }
          </li>
        }
      </ol>
    }

    <div
      class="w-full min-w-0"
      [class.mt-6]="mode() === 'wizard'"
      [class.grid]="mode() === 'wizard'"
      [class.flex]="mode() === 'vertical'"
      [class.flex-col]="mode() === 'vertical'">
      <ng-content />
    </div>
  `,
})
export class StepperComponent {
  readonly mode = input<'vertical' | 'wizard'>('vertical');
  readonly activeIndex = model(0);
  readonly steps = contentChildren(StepComponent);

  constructor() {
    effect(() => {
      const steps = this.steps();
      const activeIndex = Math.min(
        Math.max(this.activeIndex(), 0),
        Math.max(steps.length - 1, 0)
      );

      steps.forEach((step, index) => {
        step.setState({
          index: index + 1,
          last: index === steps.length - 1,
          wizard: this.mode() === 'wizard',
          active: index === activeIndex,
        });
      });
    });
  }
}
