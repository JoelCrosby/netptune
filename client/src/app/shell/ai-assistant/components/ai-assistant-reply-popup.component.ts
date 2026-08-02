import { Component, input, output } from '@angular/core';
import { LucideSparkles, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-ai-assistant-reply-popup',
  imports: [LucideSparkles, LucideTriangleAlert, LucideX, IconButtonComponent],
  template: `
    <div
      class="reply-popup bg-card border-border w-80 max-w-[calc(100vw-2rem)] rounded-lg border p-3 shadow-lg"
      role="status"
      aria-live="polite">
      <div class="flex items-start gap-2">
        <span
          class="bg-hover text-muted flex h-7 w-7 shrink-0 items-center justify-center rounded-lg">
          @if (failed()) {
            <svg lucideTriangleAlert class="h-4 w-4"></svg>
          } @else {
            <svg lucideSparkles class="h-4 w-4"></svg>
          }
        </span>

        <div class="min-w-0 flex-1">
          @if (failed()) {
            <p
              class="text-sm font-medium"
              i18n="
                Heading of the popup shown when an assistant turn failed while
                the chat was closed
              ">
              The assistant stopped
            </p>
          } @else {
            <p
              class="text-sm font-medium"
              i18n="
                Heading of the popup shown when the assistant replies while the
                chat is closed
              ">
              The assistant replied
            </p>
          }

          @if (preview()) {
            <p class="text-muted mt-0.5 line-clamp-3 text-sm">
              {{ preview() }}
            </p>
          }

          <button
            type="button"
            class="text-primary mt-2 text-sm font-medium hover:underline"
            i18n="Button that opens the assistant from the new reply popup"
            (click)="opened.emit()">
            Open chat
          </button>
        </div>

        <button
          app-icon-button
          type="button"
          class="-mt-1 -mr-1 shrink-0 rounded-full"
          i18n-aria-label="
            Accessible label for the button that hides the new reply popup
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
      @keyframes reply-popup-in {
        from {
          opacity: 0;
          transform: scale(0.96) translateY(-6px);
        }
        to {
          opacity: 1;
          transform: scale(1) translateY(0);
        }
      }

      .reply-popup {
        animation: reply-popup-in 140ms ease-out;
        transform-origin: top right;
      }
    `,
  ],
})
export class AiAssistantReplyPopupComponent {
  readonly preview = input('');
  readonly failed = input(false);

  readonly opened = output();
  readonly dismissed = output();
}
