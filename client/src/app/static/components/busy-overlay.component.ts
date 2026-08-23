import { booleanAttribute, Component, input } from '@angular/core';
import { SpinnerComponent } from './spinner/spinner.component';

@Component({
  selector: 'app-busy-overlay',
  imports: [SpinnerComponent],
  host: { class: 'relative block', '[attr.aria-busy]': 'busy()' },
  template: `
    <div
      class="transition-opacity"
      [class.opacity-40]="busy()"
      [attr.inert]="busy() ? '' : null">
      <ng-content />
    </div>

    @if (busy()) {
      <div
        class="absolute inset-0 flex flex-col items-center justify-center gap-3">
        <app-spinner [diameter]="spinnerDiameter()" />

        @if (message(); as message) {
          <p class="text-muted text-sm">{{ message }}</p>
        }
      </div>
    }
  `,
})
export class BusyOverlayComponent {
  readonly busy = input(false, { transform: booleanAttribute });
  readonly message = input('');
  readonly spinnerDiameter = input('2.5rem');
}
