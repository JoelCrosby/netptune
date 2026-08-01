import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ClientResponse } from '@core/models/client-response';
import {
  AiConversationDetail,
  AiMessageRole,
} from '@core/models/ai-conversation';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { aiWorkspaceConversationResource } from '@core/resources/ai-workspace-conversation.resource';
import { LucideArrowLeft } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';

@Component({
  selector: 'app-assistant-conversations-view',
  imports: [
    LucideArrowLeft,
    EmptyStateComponent,
    IconButtonComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PrettyDatePipe,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the workspace assistant conversations"
        title="Assistant conversations" />

      <p
        class="text-muted mb-4 max-w-3xl text-sm"
        i18n="Explains what an admin sees on the assistant conversations page">
        Conversations members have had with the assistant in this workspace. The
        record of what was actually changed lives in the audit log.
      </p>

      @if (selected(); as detail) {
        <div class="mb-3 flex items-center gap-2">
          <button app-icon-button type="button" (click)="clearSelection()">
            <svg lucideArrowLeft class="h-4 w-4"></svg>
          </button>
          <h3 class="font-overpass text-[1.05rem] font-normal">
            {{ detail.conversation.title }}
          </h3>
        </div>

        <div class="flex flex-col gap-4">
          @for (message of detail.messages; track message.id) {
            <div class="flex flex-col gap-1">
              <span class="text-muted text-xs">
                @if (message.role === userRole) {
                  <span
                    i18n="
                      Label for a message sent by the member whose conversation
                      an admin is reading
                    "
                    >Member</span
                  >
                } @else {
                  <span i18n="Label for a message the assistant sent"
                    >Assistant</span
                  >
                }
              </span>
              <p class="text-sm whitespace-pre-wrap">{{ message.text }}</p>
            </div>
          }
        </div>
      } @else {
        <div class="flex flex-col">
          @for (conversation of conversations.value(); track conversation.id) {
            <button
              type="button"
              class="border-border flex items-center justify-between gap-4 border-b py-3 text-left"
              (click)="select(conversation)">
              <span class="min-w-0">
                <span class="block truncate text-sm">{{
                  conversation.title
                }}</span>
                <span class="text-muted text-xs">
                  {{ conversation.userDisplayName }} ·
                  {{ conversation.messageCount }}
                  <span i18n="Counts messages in a stored conversation"
                    >messages</span
                  >
                </span>
              </span>
              <span class="text-muted shrink-0 text-xs">
                {{ toDate(conversation.lastMessageAt) | prettyDate }}
              </span>
            </button>
          } @empty {
            <app-empty-state
              compact
              i18n-title="Heading when no assistant conversations exist"
              title="There are no conversations"
              i18n-description="
                Explains why the assistant conversation list is empty
              "
              description="Conversations appear here once members use the assistant" />
          }
        </div>
      }
    </app-page-container>
  `,
})
export class AssistantConversationsViewComponent {
  private readonly http = inject(HttpClient);

  protected readonly conversations = aiWorkspaceConversationResource();
  protected readonly selected = signal<AiConversationDetail | null>(null);
  protected readonly userRole = AiMessageRole.user;

  protected select(conversation: AiWorkspaceConversation) {
    this.http
      .get<ClientResponse<AiConversationDetail>>(
        `api/ai/admin/conversations/${conversation.id}`
      )
      .subscribe((response) => this.selected.set(response.payload ?? null));
  }

  protected toDate(value: string): Date {
    return new Date(value);
  }

  protected clearSelection() {
    this.selected.set(null);
  }
}
