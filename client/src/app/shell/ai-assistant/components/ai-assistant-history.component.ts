import { Component, input, output } from '@angular/core';
import { AiConversation } from '@core/models/ai-conversation';
import { formatTokens } from '@core/util/ai-usage';
import { LucideTrash } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-ai-assistant-history',
  host: { class: 'block' },
  imports: [LucideTrash, IconButtonComponent],
  template: `
    @for (conversation of conversations(); track conversation.id) {
      <div class="border-border flex items-center gap-2 border-b py-2">
        <button
          type="button"
          class="min-w-0 flex-1 text-left text-sm hover:underline"
          (click)="opened.emit(conversation.id)">
          <span class="block truncate">{{ conversation.title }}</span>
          <span class="text-muted text-xs">
            {{ conversation.messageCount }}
            <span i18n="Counts messages in a stored conversation"
              >messages</span
            >
            · {{ tokenLabel(conversation) }}
            <span i18n="Counts tokens a conversation has cost">tokens</span>
          </span>
        </button>
        <button
          app-icon-button
          type="button"
          class="rounded-full"
          (click)="deleted.emit(conversation.id)">
          <svg lucideTrash class="h-4 w-4"></svg>
        </button>
      </div>
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
