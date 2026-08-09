import { Component, output } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-anchored-popup-card',
  imports: [LucideX, IconButtonComponent],
  template: `
    <div
      class="anchored-popup bg-card border-border w-80 max-w-[calc(100vw-2rem)] rounded-lg border p-3 shadow-lg"
      role="status"
      aria-live="polite">
      <div class="flex items-start gap-2">
        <span
          class="bg-hover text-muted flex h-7 w-7 shrink-0 items-center justify-center rounded-lg">
          <ng-content select="[popupIcon]" />
        </span>

        <div class="min-w-0 flex-1">
          <ng-content />
        </div>

        <button
          app-icon-button
          type="button"
          class="-mt-1 -mr-1 shrink-0 rounded-full"
          i18n-aria-label="
            Accessible label for the button that hides a popup notice
          "
          aria-label="Dismiss"
          (click)="dismissed.emit()">
          <svg lucideX class="h-4 w-4"></svg>
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      @keyframes anchored-popup-in {
        from {
          opacity: 0;
          transform: scale(0.96) translateY(-6px);
        }
        to {
          opacity: 1;
          transform: scale(1) translateY(0);
        }
      }

      .anchored-popup {
        animation: anchored-popup-in 140ms ease-out;
        transform-origin: top right;
      }
    `,
  ],
})
export class AnchoredPopupCardComponent {
  readonly dismissed = output();
}
