import { booleanAttribute, Component, input, output } from '@angular/core';
import { LucideTriangleAlert } from '@lucide/angular';
import { StrokedButtonComponent } from '../button/stroked-button.component';

@Component({
  selector: 'app-error-state',
  imports: [LucideTriangleAlert, StrokedButtonComponent],
  host: { class: 'block' },
  template: `
    <div
      role="alert"
      class="flex flex-col items-center justify-center gap-2 text-center"
      [class]="compact() ? 'min-h-32 py-4' : 'my-10 h-full'">
      <svg
        lucideTriangleAlert
        class="text-warn h-6 w-6"
        aria-hidden="true"></svg>

      <h4 [class]="compact() ? 'text-sm font-medium' : 'mx-8 font-normal'">
        {{ title() }}
      </h4>

      @if (description()) {
        <p
          class="text-sm"
          [class]="
            compact() ? 'text-foreground/60' : 'text-foreground/70 mb-2'
          ">
          {{ description() }}
        </p>
      }

      @if (retryable()) {
        <button
          app-stroked-button
          class="mt-2"
          type="button"
          [disabled]="retrying()"
          (click)="retry.emit()">
          {{ retrying() ? 'Retrying…' : retryLabel() }}
        </button>
      }
    </div>
  `,
})
export class ErrorStateComponent {
  readonly title = input('Something went wrong');
  readonly description = input('');
  readonly retryLabel = input('Try again');
  readonly retryable = input(true, { transform: booleanAttribute });
  readonly retrying = input(false, { transform: booleanAttribute });
  readonly compact = input(false, { transform: booleanAttribute });

  readonly retry = output();
}
