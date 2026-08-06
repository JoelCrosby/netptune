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
      <ol
        class="m-0 flex list-none items-start p-0"
        i18n-aria-label="
          Accessible label for the multi-step form progress indicator
        "
        aria-label="Form progress">
        @for (step of steps(); track step; let index = $index, last = $last) {
          <li class="flex min-w-0 items-center" [class.flex-1]="!last">
            <div class="flex min-w-0 flex-col items-center gap-2 text-center">
              <span
                [class]="markerClass(index)"
                [attr.aria-current]="
                  index === activeIndex() ? 'step' : undefined
                ">
                {{ index + 1 }}
              </span>
              <span
                class="max-w-28 truncate text-xs"
                [class]="labelClass(index)">
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

  // The step you are on has to be the most prominent marker in the rail. Painting completed steps
  // solid and the current one as a faint outline read backwards: the last completed step looked
  // active, so people thought they were a step behind where they actually were.
  protected markerClass(index: number): string {
    const base =
      'flex h-8 w-8 items-center justify-center rounded-full border text-sm font-medium transition-colors';
    const active = this.activeIndex();

    if (index === active) {
      return `${base} border-primary bg-primary text-background ring-primary/30 ring-4`;
    }

    if (index < active) {
      return `${base} border-primary/40 bg-primary/15 text-primary`;
    }

    return `${base} border-border bg-background text-muted`;
  }

  protected labelClass(index: number): string {
    return index === this.activeIndex()
      ? 'text-foreground font-medium'
      : 'text-muted';
  }

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
