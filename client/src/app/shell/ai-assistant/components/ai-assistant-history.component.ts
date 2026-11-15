import { Component, inject, viewChild } from '@angular/core';
import { AiConversation } from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { formatTokens } from '@core/util/ai-usage';
import {
  LucideCheck,
  LucideHistory,
  LucideMessageSquare,
  LucideTrash,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant-history',
  host: { class: 'inline-flex' },
  imports: [
    LucideCheck,
    LucideHistory,
    LucideMessageSquare,
    LucideTrash,
    DropdownMenuComponent,
    IconButtonComponent,
    MenuItemComponent,
    TooltipDirective,
  ],
  template: `
    <span #trigger class="inline-flex">
      <button
        app-icon-button
        type="button"
        color="muted"
        size="small"
        aria-haspopup="menu"
        i18n-appTooltip="Tooltip on the button that lists past conversations"
        appTooltip="Conversation history"
        appTooltipPosition="bottom"
        (click)="toggle(trigger)">
        <svg lucideHistory class="h-4 w-4"></svg>
      </button>
    </span>

    <app-dropdown-menu
      #menu
      xPosition="before"
      panelClass="custom-scroll w-76 max-h-[min(24rem,60vh)] overflow-x-hidden overflow-y-auto p-1">
      @for (conversation of conversations(); track conversation.id) {
        <div class="relative flex items-center">
          <button
            app-menu-item
            type="button"
            class="gap-2.5 pr-10"
            [attr.aria-current]="isCurrent(conversation) ? 'true' : null"
            (click)="select(conversation.id)">
            <span class="flex h-4 w-4 shrink-0 items-center justify-center">
              @if (isCurrent(conversation)) {
                <svg lucideCheck class="h-4 w-4"></svg>
              } @else {
                <svg lucideMessageSquare class="h-4 w-4"></svg>
              }
            </span>
            <span class="flex min-w-0 flex-1 flex-col">
              <span class="truncate">{{ conversation.title }}</span>
              <span class="text-muted truncate text-xs">
                {{ metaLabel(conversation) }}
              </span>
            </span>
          </button>

          <button
            app-icon-button
            type="button"
            color="muted"
            size="small"
            class="absolute right-1 rounded-full"
            i18n-aria-label="
              Accessible label for the button that deletes a stored conversation
            "
            aria-label="Delete conversation"
            (click)="remove(conversation.id)">
            <svg lucideTrash class="h-4 w-4"></svg>
          </button>
        </div>
      } @empty {
        <p
          class="text-muted px-3 py-2 text-sm"
          i18n="Empty state for stored conversations">
          There are no earlier conversations.
        </p>
      }

      @if (hasMore()) {
        <button
          app-menu-item
          type="button"
          class="text-muted justify-center text-xs"
          (click)="loadMore()">
          <span
            i18n="Button that loads more of the stored assistant conversations">
            Load more
          </span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class AiAssistantHistoryComponent {
  private readonly assistant = inject(AiAssistantService);
  private readonly menu = viewChild.required(DropdownMenuComponent);

  protected readonly conversations = this.assistant.conversations;
  protected readonly hasMore = this.assistant.hasMoreConversations;

  protected toggle(origin: HTMLElement) {
    const menu = this.menu();

    menu.toggle(origin);

    if (menu.showing()) {
      void this.assistant.loadConversations();
    }
  }

  protected isCurrent(conversation: AiConversation): boolean {
    return conversation.id === this.assistant.conversationId();
  }

  protected metaLabel(conversation: AiConversation): string {
    const messages = conversation.messageCount;
    const tokens = formatTokens(conversation.usage);

    return $localize`:What a stored conversation holds. COUNT is a number of messages, TOKENS a token count:${messages}:COUNT: messages · ${tokens}:TOKENS: tokens`;
  }

  protected select(conversationId: string) {
    this.menu().close();

    void this.assistant.openConversation(conversationId);
  }

  protected remove(conversationId: string) {
    this.menu().close();

    void this.assistant.deleteConversation(conversationId);
  }

  protected loadMore() {
    void this.assistant.loadMoreConversations();
  }
}
