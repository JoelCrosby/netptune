import { DOCUMENT } from '@angular/common';
import {
  afterRenderEffect,
  booleanAttribute,
  Component,
  ElementRef,
  inject,
  input,
} from '@angular/core';
import { SpinnerComponent } from './spinner/spinner.component';

@Component({
  selector: 'app-busy-overlay',
  imports: [SpinnerComponent],
  host: { class: 'relative block', '[attr.aria-busy]': 'busy()' },
  template: `
    <div
      class="transition-opacity"
      [class.opacity-40]="busy()"
      [attr.inert]="busy() ? '' : null"
      (focusin)="onContentFocusIn($event)"
      (focusout)="onContentFocusOut($event)">
      <ng-content />
    </div>

    <span class="sr-only" role="status">{{ busy() ? message() : '' }}</span>

    @if (busy()) {
      <div
        class="absolute inset-0 flex flex-col items-center justify-center gap-3">
        <app-spinner [diameter]="spinnerDiameter()" />

        @if (message(); as message) {
          <p class="text-muted text-sm" aria-hidden="true">{{ message }}</p>
        }
      </div>
    }
  `,
})
export class BusyOverlayComponent {
  private readonly document = inject(DOCUMENT);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly busy = input(false, { transform: booleanAttribute });
  readonly message = input('');
  readonly spinnerDiameter = input('2.5rem');

  private lastFocused: HTMLElement | null = null;
  private wasBusy = false;

  constructor() {
    afterRenderEffect(() => {
      const busy = this.busy();
      const finished = this.wasBusy && !busy;
      this.wasBusy = busy;

      if (!finished) return;

      const active = this.document.activeElement;
      const focusLost = !active || active === this.document.body;

      if (focusLost && this.lastFocused?.isConnected) this.lastFocused.focus();
    });
  }

  protected onContentFocusIn(event: FocusEvent) {
    if (event.target instanceof HTMLElement) this.lastFocused = event.target;
  }

  protected onContentFocusOut(event: FocusEvent) {
    const next = event.relatedTarget;

    if (next instanceof Node && !this.host.nativeElement.contains(next)) {
      this.lastFocused = null;
    }
  }
}
