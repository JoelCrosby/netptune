import { Component, inject } from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { LucideSparkles } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant-button',
  imports: [IconButtonComponent, LucideSparkles, TooltipDirective],
  template: `
    @if (assistant.isAvailable()) {
      <button
        app-icon-button
        type="button"
        class="relative rounded-full"
        [class.text-primary]="assistant.isOpen()"
        [attr.aria-pressed]="assistant.isOpen()"
        i18n-aria-label="
          Accessible label for the button that opens the assistant
        "
        aria-label="Assistant"
        appTooltip
        appTooltipPosition="bottom"
        i18n-appTooltip="
          Tooltip on the button that opens the assistant. Translate the modifier
          key to its local name (for example Strg in German); leave the I as-is
        "
        appTooltip="Assistant · Ctrl I"
        (click)="assistant.toggle()">
        <svg lucideSparkles class="h-4 w-4"></svg>

        @if (assistant.hasUnreadReply()) {
          <span
            aria-hidden="true"
            class="bg-primary border-background absolute top-1.5 right-1.5 h-2 w-2 rounded-full border"></span>
          <span
            class="sr-only"
            i18n="
              Announced when the assistant has replied while the chat is closed
            ">
            New assistant reply
          </span>
        }
      </button>
    }
  `,
})
export class AiAssistantButtonComponent {
  protected readonly assistant = inject(AiAssistantService);
}
