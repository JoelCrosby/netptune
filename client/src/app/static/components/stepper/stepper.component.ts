import { Component, contentChildren, effect } from '@angular/core';
import { StepComponent } from './step.component';

/**
 * Static vertical stepper. Displays projected `app-step` children in order,
 * numbering them and drawing the connecting rail. Non-interactive: every step
 * is always visible.
 */
@Component({
  selector: 'app-stepper',
  template: `
    <div class="flex flex-col">
      <ng-content />
    </div>
  `,
})
export class StepperComponent {
  private readonly steps = contentChildren(StepComponent);

  constructor() {
    effect(() => {
      const steps = this.steps();
      steps.forEach((step, index) =>
        step.setPosition(index + 1, index === steps.length - 1)
      );
    });
  }
}
