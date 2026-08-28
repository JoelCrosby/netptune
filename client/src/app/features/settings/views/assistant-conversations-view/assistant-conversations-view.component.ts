import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { AiAssistantMessageComponent } from '@app/shell/ai-assistant/components/ai-assistant-message.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiConversationDetail } from '@core/models/ai-conversation';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { ClientResponse } from '@core/models/client-response';
import { aiWorkspaceConversationResource } from '@core/resources/ai-workspace-conversation.resource';
import { toChatEntries } from '@core/models/ai-chat-entry';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { referenceMap } from '@core/util/ai-references';
import { formatCost, formatTokens, sumUsage } from '@core/util/ai-usage';
import {
  LucideArrowLeft,
  LucideKeyRound,
  LucideMessagesSquare,
  LucideGlobe,
  LucideSparkles,
  LucideWallet,
} from '@lucide/angular';
import { AiCredentialsComponent } from '@settings/components/ai-credentials/ai-credentials.component';
import { SearchCredentialComponent } from '@settings/components/search-credential/search-credential.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { SwitchComponent } from '@static/components/switch/switch.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';

@Component({
  selector: 'app-assistant-conversations-view',
  imports: [
    AiAssistantMessageComponent,
    AiCredentialsComponent,
    SearchCredentialComponent,
    EmptyStateComponent,
    IconButtonComponent,
    IconTileComponent,
    LucideArrowLeft,
    LucideMessagesSquare,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PrettyDatePipe,
    SkeletonComponent,
    StatStripComponent,
    SwitchComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the workspace assistant conversations"
        title="Assistant conversations" />

      <app-page-body scroll>
        @if (selected(); as detail) {
          <section
            class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
            <header
              class="border-border flex items-start gap-3 border-b px-6 py-5">
              <button
                app-icon-button
                class="mt-0.5 h-8 w-8 shrink-0"
                type="button"
                i18n-aria-label="
                  Accessible label for the button that leaves a conversation
                "
                aria-label="Back to conversations"
                (click)="clearSelection()">
                <svg lucideArrowLeft class="h-4 w-4"></svg>
              </button>

              <div class="min-w-0">
                <h2 class="font-overpass truncate text-base font-semibold">
                  {{ detail.conversation.title }}
                </h2>
                <p class="text-muted mt-1 text-xs">
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
                  <span
                    i18n="Counts tokens written to the provider prompt cache"
                    >written</span
                  >
                  · {{ detailCostLabel() }}
                </p>
              </div>
            </header>

            <div class="flex flex-col gap-5 px-6 py-5">
              @for (entry of transcript(); track $index) {
                <app-ai-assistant-message
                  [entry]="entry"
                  [references]="references()"
                  [workspace]="workspaceKey()" />
              }
            </div>
          </section>
        } @else {
          <div class="flex flex-col gap-6">
            @if (canUpdateWorkspace()) {
              <section
                class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
                <div
                  class="flex flex-wrap items-center justify-between gap-x-4 gap-y-3 px-6 py-5">
                  <div class="flex min-w-0 items-start gap-3">
                    <app-icon-tile [icon]="assistantIcon" />

                    <div class="min-w-0">
                      <h2
                        class="font-overpass text-base font-semibold"
                        i18n="Heading of the assistant access card">
                        Assistant access
                      </h2>
                      <p
                        class="text-muted mt-1 text-sm"
                        i18n="Explains what turning the assistant off does">
                        Turning this off stops new assistant messages and blocks
                        pending changes from being applied.
                      </p>
                    </div>
                  </div>

                  <app-switch
                    class="shrink-0"
                    [checked]="assistantEnabled()"
                    i18n-ariaLabel="
                      Toggle that enables the assistant for a workspace
                    "
                    ariaLabel="Allow members to use the assistant"
                    (changed)="setAssistantEnabled($event)" />
                </div>

                <div
                  class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-t px-6 py-5">
                  <div class="min-w-0">
                    <h3
                      class="text-sm font-medium"
                      i18n="Heading of the assistant data sampling setting">
                      Share example values with the assistant
                    </h3>
                    <p
                      class="text-muted mt-1 text-sm"
                      i18n="
                        Explains what turning off assistant data sampling does
                      ">
                      When an import mapping is improved by the assistant, a few
                      real cell values are sent with the column names. Turn this
                      off to send column names and types only.
                    </p>
                  </div>

                  <app-switch
                    class="shrink-0"
                    [checked]="allowsDataSampling()"
                    i18n-ariaLabel="
                      Toggle that shares example values with the assistant
                    "
                    ariaLabel="Share example values with the assistant"
                    (changed)="setAllowDataSampling($event)" />
                </div>
              </section>

              <section>
                <div class="mb-4 flex min-w-0 items-center gap-3">
                  <app-icon-tile [icon]="keyIcon" />
                  <h2
                    class="font-overpass text-base font-semibold"
                    i18n="Section heading for the shared workspace API keys">
                    Workspace keys
                  </h2>
                </div>

                <app-ai-credentials scope="workspace" />
              </section>

              <section>
                <div class="mb-4 flex min-w-0 items-center gap-3">
                  <app-icon-tile [icon]="searchIcon" />
                  <h2
                    class="font-overpass text-base font-semibold"
                    i18n="
                      Section heading for the workspace web search provider
                    ">
                    Web search
                  </h2>
                </div>

                <app-search-credential />
              </section>
            }

            @if (hasSpend()) {
              <section
                class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
                <header class="border-border border-b px-6 py-5">
                  <div class="flex min-w-0 items-center gap-3">
                    <app-icon-tile [icon]="spendIcon" />

                    <div class="min-w-0">
                      <h2
                        class="font-overpass text-base font-semibold"
                        i18n="
                          Section heading for what the assistant has cost the
                          workspace
                        ">
                        Assistant spend
                      </h2>
                      <p
                        class="text-muted mt-1 text-sm"
                        i18n="Explains how assistant cost is worked out">
                        Estimated from published model rates.
                      </p>
                    </div>
                  </div>
                </header>

                <app-stat-strip [items]="spendStats()" />
              </section>
            }

            <section
              class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
              <header class="border-border border-b px-6 py-5">
                <div class="flex min-w-0 items-center gap-3">
                  <app-icon-tile [icon]="conversationIcon" />

                  <div class="min-w-0">
                    <h2
                      class="font-overpass text-base font-semibold"
                      i18n="Heading of the assistant conversation list">
                      Conversations
                    </h2>
                    <p
                      class="text-muted mt-1 text-sm"
                      i18n="
                        Explains what an admin sees on the assistant
                        conversations page
                      ">
                      What members asked the assistant. The record of what
                      changed lives in the audit log.
                    </p>
                  </div>
                </div>
              </header>

              @if (isInitialLoad()) {
                <div
                  class="flex flex-col gap-4 px-6 py-5"
                  role="status"
                  i18n-aria-label="Accessible label while conversations load"
                  aria-label="Loading conversations">
                  @for (row of skeletonRows; track $index) {
                    <div class="flex items-center gap-3">
                      <app-skeleton class="h-8 w-8 shrink-0 rounded-lg" />
                      <div class="flex-1">
                        <app-skeleton class="h-3 w-48" />
                        <app-skeleton class="mt-2 h-3 w-72" />
                      </div>
                    </div>
                  }
                </div>
              } @else {
                <ul class="divide-border/50 flex flex-col divide-y">
                  @for (
                    conversation of conversations.value();
                    track conversation.id
                  ) {
                    <li>
                      <button
                        type="button"
                        class="hover:bg-hover focus-visible:ring-primary flex w-full items-center gap-3 px-6 py-4 text-left transition-colors focus-visible:ring-2 focus-visible:-outline-offset-2 focus-visible:outline-none"
                        (click)="select(conversation)">
                        <app-icon-tile
                          size="small"
                          [icon]="conversationIcon"
                          class="mt-0.5" />

                        <span class="min-w-0 flex-1">
                          <span class="block truncate text-sm font-medium">
                            {{ conversation.title }}
                          </span>
                          <span class="text-muted block truncate text-xs">
                            {{ conversation.userDisplayName }} ·
                            {{ conversation.messageCount }}
                            <span
                              i18n="Counts messages in a stored conversation"
                              >messages</span
                            >
                            · {{ tokenLabel(conversation) }}
                            <span i18n="Counts tokens a conversation has cost"
                              >tokens</span
                            >
                            · {{ costLabel(conversation) }}
                          </span>
                        </span>

                        <span class="text-muted shrink-0 text-xs">
                          {{ toDate(conversation.lastMessageAt) | prettyDate }}
                        </span>
                      </button>
                    </li>
                  } @empty {
                    <li>
                      <app-empty-state
                        compact
                        i18n-title="
                          Heading when no assistant conversations exist
                        "
                        title="There are no conversations"
                        i18n-description="
                          Explains why the assistant conversation list is empty
                        "
                        description="Conversations appear here once members use the assistant">
                        <svg
                          emptyStateIcon
                          lucideMessagesSquare
                          class="h-8 w-8"></svg>
                      </app-empty-state>
                    </li>
                  }
                </ul>
              }
            </section>
          </div>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class AssistantConversationsViewComponent {
  private readonly http = inject(HttpClient);
  private workspaceCommands = inject(WorkspaceCommandsService);
  private readonly workspace = inject(CurrentWorkspaceService).workspace;

  protected readonly conversations = aiWorkspaceConversationResource();
  protected readonly selected = signal<AiConversationDetail | null>(null);
  protected readonly selectedMember = signal<string | null>(null);

  protected readonly assistantIcon = LucideSparkles;
  protected readonly keyIcon = LucideKeyRound;
  protected readonly searchIcon = LucideGlobe;
  protected readonly spendIcon = LucideWallet;
  protected readonly conversationIcon = LucideMessagesSquare;
  protected readonly skeletonRows = Array.from({ length: 4 });

  private readonly workspaceIdentifier = inject(CurrentWorkspaceService).slug;

  protected readonly workspaceKey = computed(() => {
    return this.workspaceIdentifier() ?? null;
  });

  protected readonly isInitialLoad = computed(() => {
    return this.conversations.isLoading() && this.conversationCount() === 0;
  });

  protected readonly detailCostLabel = computed(() => {
    return formatCost(this.selected()?.conversation.usage);
  });

  protected readonly transcript = computed(() => {
    const messages = this.selected()?.messages ?? [];

    return toChatEntries(messages);
  });

  protected readonly references = computed(() => {
    const messages = this.selected()?.messages ?? [];

    return referenceMap(messages.flatMap((message) => message.references));
  });

  protected readonly canUpdateWorkspace = hasPermission(
    PERMISSIONS.workspace.update
  );

  protected readonly assistantEnabled = computed(() => {
    return this.workspace()?.assistantEnabled !== false;
  });

  protected readonly allowsDataSampling = computed(() => {
    return this.workspace()?.allowAssistantDataSampling !== false;
  });

  protected setAllowDataSampling(allowed: boolean) {
    const current = this.workspace();

    if (!current) {
      return;
    }

    this.workspaceCommands.edit({
      slug: current.slug,
      metaInfo: current.metaInfo ?? {},
      allowAssistantDataSampling: allowed,
    });
  }

  protected setAssistantEnabled(enabled: boolean) {
    const current = this.workspace();

    if (!current) {
      return;
    }

    this.workspaceCommands.edit({
      slug: current.slug,
      metaInfo: current.metaInfo ?? {},
      assistantEnabled: enabled,
    });
  }

  protected readonly workspaceUsage = computed(() => {
    const conversations = this.conversations.value() ?? [];

    return sumUsage(conversations.map((conversation) => conversation.usage));
  });

  protected readonly conversationCount = computed(() => {
    return this.conversations.value()?.length ?? 0;
  });

  protected readonly hasSpend = computed(() => this.conversationCount() > 0);

  protected readonly spendStats = computed<StatStripItem[]>(() => {
    return [
      {
        label: $localize`:Label for the number of assistant conversations:Conversations`,
        value: this.conversationCount(),
      },
      {
        label: $localize`:Label for the number of tokens the assistant has used:Tokens`,
        value: formatTokens(this.workspaceUsage()),
      },
      {
        label: $localize`:Label for what the assistant has cost, priced from published model rates:Estimated cost`,
        value: formatCost(this.workspaceUsage()),
      },
    ];
  });

  protected tokenLabel(conversation: AiWorkspaceConversation): string {
    return formatTokens(conversation.usage);
  }

  protected costLabel(conversation: AiWorkspaceConversation): string {
    return formatCost(conversation.usage);
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
