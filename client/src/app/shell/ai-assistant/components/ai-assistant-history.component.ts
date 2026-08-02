import { Component, input, output } from '@angular/core';
import { AiConversation } from '@core/models/ai-conversation';
import { formatTokens } from '@core/util/ai-usage';
import { LucideMessageSquare, LucideTrash } from '@lucide/angular';
import { ActionCardComponent } from '@static/components/action-card/action-card.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-ai-assistant-history',
  host: { class: 'flex flex-col gap-2' },
  imports: [
    LucideMessageSquare,
    LucideTrash,
    ActionCardComponent,
    IconButtonComponent,
  ],
  template: `
    @for (conversation of conversations(); track conversation.id) {
      <app-action-card
        [heading]="conversation.title"
        (activated)="opened.emit(conversation.id)">
        <svg actionCardIcon lucideMessageSquare class="h-4 w-4"></svg>

        {{ conversation.messageCount }}
        <span i18n="Counts messages in a stored conversation">messages</span>
        · {{ tokenLabel(conversation) }}
        <span i18n="Counts tokens a conversation has cost">tokens</span>

        <button
          actionCardTrailing
          app-icon-button
          type="button"
          class="pointer-events-auto -my-1 rounded-full"
          i18n-aria-label="
            Accessible label for the button that deletes a stored conversation
          "
          aria-label="Delete conversation"
          (click)="deleted.emit(conversation.id)">
          <svg lucideTrash class="h-4 w-4"></svg>
        </button>
      </app-action-card>
    } @empty {
      <p class="text-muted text-sm" i18n="Empty state for stored conversations">
        There are no earlier conversations.
      </p>
    }
  `,
})
export class AiAssistantHistoryComponent {
  readonly conversations = input.required<AiConversation[]>();

  readonly opened = output<string>();
  readonly deleted = output<string>();

  protected tokenLabel(conversation: AiConversation): string {
    return formatTokens(conversation.usage);
  }
}
