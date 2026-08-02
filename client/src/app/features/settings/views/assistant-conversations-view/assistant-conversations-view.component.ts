import { Component, computed, inject, signal } from '@angular/core';
import { AiAssistantMessageComponent } from '@app/shell/ai-assistant/components/ai-assistant-message.component';
import { HttpClient } from '@angular/common/http';
import { netptunePermissions } from '@core/auth/permissions';
import { editWorkspace } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { ClientResponse } from '@core/models/client-response';
import { AiConversationDetail } from '@core/models/ai-conversation';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { referenceMap } from '@core/util/ai-references';
import { toChatEntry } from '@core/services/ai-assistant.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { aiWorkspaceConversationResource } from '@core/resources/ai-workspace-conversation.resource';
import { formatTokens } from '@core/util/ai-usage';
import { LucideArrowLeft, LucideMessageSquare } from '@lucide/angular';
import { ActionCardComponent } from '@static/components/action-card/action-card.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';

@Component({
  selector: 'app-assistant-conversations-view',
  imports: [
    LucideArrowLeft,
    LucideMessageSquare,
    ActionCardComponent,
    AiAssistantMessageComponent,
    CheckboxComponent,
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

      @if (canUpdateWorkspace()) {
        <div class="border-border mb-6 rounded border p-4">
          <app-checkbox
            [checked]="assistantEnabled()"
            (changed)="setAssistantEnabled($event)">
            <span
              class="text-sm"
              i18n="Toggle that enables the assistant for a workspace">
              Allow members to use the assistant
            </span>
          </app-checkbox>
          <p
            class="text-muted mt-2 text-xs"
            i18n="Explains what turning the assistant off does">
            Turning this off stops new assistant messages and blocks pending
            changes from being applied.
          </p>
        </div>
      }

      @if (selected(); as detail) {
        <div class="mb-3 flex items-center gap-2">
          <button app-icon-button type="button" (click)="clearSelection()">
            <svg lucideArrowLeft class="h-4 w-4"></svg>
          </button>
          <div class="min-w-0">
            <h3 class="font-overpass truncate text-[1.05rem] font-normal">
              {{ detail.conversation.title }}
            </h3>
            <p class="text-muted text-xs">
              @if (selectedMember(); as member) {
                {{ member }} ·
              }
              {{ detail.conversation.model }} ·
              {{ detail.conversation.usage.inputTokens }}
              <span i18n="Counts tokens sent to the model">in</span> ·
              {{ detail.conversation.usage.outputTokens }}
              <span i18n="Counts tokens returned by the model">out</span> ·
              {{ detail.conversation.usage.cacheReadTokens }}
              <span i18n="Counts tokens read from the provider prompt cache"
                >cached</span
              >
              ·
              {{ detail.conversation.usage.cacheCreationTokens }}
              <span i18n="Counts tokens written to the provider prompt cache"
                >written</span
              >
            </p>
          </div>
        </div>

        <div class="flex flex-col gap-5">
          @for (entry of transcript(); track $index) {
            <app-ai-assistant-message
              [entry]="entry"
              [references]="references()"
              [workspace]="workspaceKey()" />
          }
        </div>
      } @else {
        <div class="flex flex-col gap-2">
          @for (conversation of conversations.value(); track conversation.id) {
            <app-action-card
              [heading]="conversation.title"
              (activated)="select(conversation)">
              <svg actionCardIcon lucideMessageSquare class="h-4 w-4"></svg>

              {{ conversation.userDisplayName }} ·
              {{ conversation.messageCount }}
              <span i18n="Counts messages in a stored conversation"
                >messages</span
              >
              · {{ tokenLabel(conversation) }}
              <span i18n="Counts tokens a conversation has cost">tokens</span>

              <span actionCardTrailing class="text-muted mt-0.5 text-xs">
                {{ toDate(conversation.lastMessageAt) | prettyDate }}
              </span>
            </app-action-card>
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
  private readonly store = inject(Store);
  private readonly workspace = this.store.selectSignal(selectCurrentWorkspace);

  protected readonly conversations = aiWorkspaceConversationResource();
  protected readonly selected = signal<AiConversationDetail | null>(null);
  protected readonly selectedMember = signal<string | null>(null);

  private readonly workspaceIdentifier = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  protected readonly workspaceKey = computed(() => {
    return this.workspaceIdentifier() ?? null;
  });

  protected readonly transcript = computed(() => {
    const messages = this.selected()?.messages ?? [];

    return messages.map(toChatEntry);
  });

  protected readonly references = computed(() => {
    const messages = this.selected()?.messages ?? [];

    return referenceMap(messages.flatMap((message) => message.references));
  });

  protected readonly canUpdateWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.update)
  );

  protected readonly assistantEnabled = computed(() => {
    return this.workspace()?.assistantEnabled !== false;
  });

  protected setAssistantEnabled(enabled: boolean) {
    const current = this.workspace();

    if (!current) {
      return;
    }

    this.store.dispatch(
      editWorkspace.init({
        request: {
          slug: current.slug,
          metaInfo: current.metaInfo ?? {},
          assistantEnabled: enabled,
        },
      })
    );
  }

  protected tokenLabel(conversation: AiWorkspaceConversation): string {
    return formatTokens(conversation.usage);
  }

  protected select(conversation: AiWorkspaceConversation) {
    this.selectedMember.set(conversation.userDisplayName);

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
    this.selectedMember.set(null);
  }
}
