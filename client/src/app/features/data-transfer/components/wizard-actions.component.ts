import { Component, computed, input, output } from '@angular/core';
import { LucideArrowLeft, LucideArrowRight } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-wizard-actions',
  imports: [
    FlatButtonComponent,
    LucideArrowLeft,
    LucideArrowRight,
    StrokedButtonComponent,
  ],
  host: {
    class:
      'border-border bg-card sticky bottom-0 z-10 -mx-6 -mb-5 mt-6 flex flex-wrap items-center justify-between gap-3 border-t px-6 pt-4 pb-5',
  },
  template: `
    <button
      app-stroked-button
      type="button"
      [disabled]="activeIndex() === 0"
      (click)="back.emit()">
      <svg lucideArrowLeft class="h-4 w-4"></svg>
      <span i18n="Button that moves back one wizard step">Back</span>
    </button>

    <div class="flex min-w-0 flex-wrap items-center justify-end gap-3">
      <ng-content select="[wizardActions]" />

      @if (nextVisible()) {
        @if (blocker(); as reason) {
          <p class="text-muted text-xs" role="status">{{ reason }}</p>
        }

        <button
          app-flat-button
          type="button"
          [disabled]="!canGoNext()"
          (click)="next.emit()">
          <span i18n="Button that moves forward one wizard step">Next</span>
          <svg lucideArrowRight class="h-4 w-4"></svg>
        </button>
      }
    </div>
  `,
})
export class WizardActionsComponent {
  readonly activeIndex = input.required<number>();
  readonly lastStepIndex = input.required<number>();
  readonly blocker = input<string | null>(null);
  readonly canGoNext = input(false);
  readonly showNext = input(true);

  readonly back = output();
  readonly next = output();

  protected readonly nextVisible = computed(() => {
    return this.showNext() && this.activeIndex() !== this.lastStepIndex();
  });
}
