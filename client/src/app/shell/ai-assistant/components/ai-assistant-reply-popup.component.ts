import { Component, input, output } from '@angular/core';
import { LucideSparkles, LucideTriangleAlert } from '@lucide/angular';
import { AnchoredPopupCardComponent } from '@static/components/anchored-popup/anchored-popup-card.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';

@Component({
  selector: 'app-ai-assistant-reply-popup',
  imports: [
    AnchoredPopupCardComponent,
    InlineButtonComponent,
    LucideSparkles,
    LucideTriangleAlert,
  ],
  template: `
    <app-anchored-popup-card (dismissed)="dismissed.emit()">
      <span popupIcon class="contents">
        @if (failed()) {
          <svg lucideTriangleAlert class="h-4 w-4"></svg>
        } @else {
          <svg lucideSparkles class="h-4 w-4"></svg>
        }
      </span>

      @if (failed()) {
        <p
          class="text-sm font-medium"
          i18n="
            Heading of the popup shown when an assistant turn failed while the
            chat was closed
          ">
          The assistant stopped
        </p>
      } @else {
        <p
          class="text-sm font-medium"
          i18n="
            Heading of the popup shown when the assistant replies while the chat
            is closed
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
        app-inline-button
        appearance="underline"
        class="mt-2 text-sm font-medium"
        i18n="Button that opens the assistant from the new reply popup"
        (click)="opened.emit()">
        Open chat
      </button>
    </app-anchored-popup-card>
  `,
})
export class AiAssistantReplyPopupComponent {
  readonly preview = input('');
  readonly failed = input(false);

  readonly opened = output();
  readonly dismissed = output();
}
