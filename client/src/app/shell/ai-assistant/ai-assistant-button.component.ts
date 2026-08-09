import {
  Component,
  ElementRef,
  TemplateRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import { summarizeAssistantMarkdown } from '@core/util/ai-markdown';
import { LucideSparkles } from '@lucide/angular';
import { anchoredPopup } from '@static/components/anchored-popup/anchored-popup';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { AiAssistantReplyPopupComponent } from './components/ai-assistant-reply-popup.component';

const POPUP_TIMEOUT = 12000;

@Component({
  selector: 'app-ai-assistant-button',
  imports: [
    IconButtonComponent,
    LucideSparkles,
    TooltipDirective,
    AiAssistantReplyPopupComponent,
  ],
  template: `
    @if (panel.isAvailable()) {
      <button
        #trigger
        app-icon-button
        type="button"
        class="relative rounded-full"
        [class.text-primary]="panel.isOpen()"
        [attr.aria-pressed]="panel.isOpen()"
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
        (click)="panel.toggle()">
        <svg lucideSparkles class="h-4 w-4"></svg>

        @if (panel.hasUnreadReply()) {
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

    <ng-template #popup>
      <app-ai-assistant-reply-popup
        [preview]="replyPreview()"
        [failed]="replyFailed()"
        (opened)="openChat()"
        (dismissed)="dismissPopup()" />
    </ng-template>
  `,
})
export class AiAssistantButtonComponent {
  protected readonly assistant = inject(AiAssistantService);
  protected readonly panel = inject(AiPanelService);

  private readonly trigger = viewChild('trigger', { read: ElementRef });
  private readonly popup = viewChild.required<TemplateRef<unknown>>('popup');

  private readonly isDismissed = signal(false);

  private readonly popupOverlay = anchoredPopup({
    timeout: POPUP_TIMEOUT,
    onTimeout: () => this.isDismissed.set(true),
  });

  private readonly lastReply = computed(() => {
    const last = this.assistant.entries().at(-1);

    if (!last || last.role !== 'assistant') {
      return null;
    }

    return last;
  });

  protected readonly replyPreview = computed(() => {
    const reply = this.lastReply();

    if (!reply) {
      return '';
    }

    return summarizeAssistantMarkdown(reply.text);
  });

  protected readonly replyFailed = computed(() => {
    return this.lastReply()?.failed === true;
  });

  constructor() {
    effect(() => {
      const hasReply = this.panel.hasUnreadReply();

      if (!hasReply) {
        this.isDismissed.set(false);
        this.hidePopup();

        return;
      }

      const isHidden = this.isDismissed();

      if (isHidden) {
        return;
      }

      this.showPopup();
    });
  }

  protected openChat() {
    this.hidePopup();
    this.panel.open();
  }

  protected dismissPopup() {
    this.isDismissed.set(true);
    this.hidePopup();
  }

  private showPopup() {
    const trigger = this.trigger();

    if (!trigger) {
      return;
    }

    this.popupOverlay.show(trigger, this.popup());
  }

  private hidePopup() {
    this.popupOverlay.hide();
  }
}
