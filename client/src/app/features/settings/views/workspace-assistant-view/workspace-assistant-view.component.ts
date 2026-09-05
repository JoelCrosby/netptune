import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiAssistantMessageComponent } from '@app/shell/ai-assistant/components/ai-assistant-message.component';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { toChatEntries } from '@core/models/ai-chat-entry';
import { AiConversationDetail } from '@core/models/ai-conversation';
import { AiProvider } from '@core/models/ai-credential';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { ClientResponse } from '@core/models/client-response';
import { aiCredentialResource } from '@core/resources/ai-credential.resource';
import { aiSpendResource } from '@core/resources/ai-spend.resource';
import { aiWorkspaceConversationResource } from '@core/resources/ai-workspace-conversation.resource';
import { searchCredentialResource } from '@core/resources/search-credential.resource';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { referenceMap } from '@core/util/ai-references';
import { formatCost, formatCurrency, formatTokens } from '@core/util/ai-usage';
import {
  LucideArrowLeft,
  LucideKeyRound,
  LucideMessagesSquare,
  LucidePlus,
  LucidePower,
  LucideShield,
  LucideTriangleAlert,
  type LucideIconInput,
} from '@lucide/angular';
import { AssistantConnectionsComponent } from '@settings/components/assistant/assistant-connections.component';
import { AssistantSpendCardComponent } from '@settings/components/assistant/assistant-spend-card.component';
import { AssistantStatusBandComponent } from '@settings/components/assistant/assistant-status-band.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SwitchComponent } from '@static/components/switch/switch.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';

interface AssistantBanner {
  icon: LucideIconInput;
  color: 'primary' | 'warn';
  title: string;
  body: string;
  action: string;
  kind: 'add-key' | 'turn-on' | 'edit-cap';
}

@Component({
  selector: 'app-workspace-assistant-view',
  imports: [
    AiAssistantMessageComponent,
    AssistantConnectionsComponent,
    AssistantSpendCardComponent,
    AssistantStatusBandComponent,
    CalloutComponent,
    DropdownMenuComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    IconButtonComponent,
    IconTileComponent,
    LucideArrowLeft,
    LucideMessagesSquare,
    LucidePlus,
    LucideShield,
    MenuItemComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PrettyDatePipe,
    RouterLink,
    SkeletonComponent,
    StrokedButtonComponent,
    SwitchComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the workspace assistant settings"
        title="Assistant">
        <div pageHeaderActions class="flex items-center gap-2">
          @if (canReadAudit()) {
            <a
              app-stroked-button
              color="neutral"
              class="h-[34px] gap-2 px-3 text-[13px]"
              [routerLink]="auditLink()">
              <svg lucideShield class="h-3.5 w-3.5"></svg>
              <span i18n="Link to the workspace audit log">Audit log</span>
            </a>
          }

          @if (canAddConnection()) {
            <button
              app-flat-button
              type="button"
              class="h-[34px] gap-2 px-3 text-[13px]"
              (click)="connectionMenu.toggle($any($event.currentTarget))">
              <svg lucidePlus class="h-3.5 w-3.5"></svg>
              <span i18n="Button that adds an assistant connection">
                Add connection
              </span>
            </button>

            <app-dropdown-menu #connectionMenu xPosition="before">
              @if (!anthropicCredential()) {
                <button
                  app-menu-item
                  type="button"
                  (click)="addProvider(anthropic); connectionMenu.close()">
                  <span i18n="Name of the Anthropic AI provider"
                    >Anthropic</span
                  >
                </button>
              }
              @if (!openAiCredential()) {
                <button
                  app-menu-item
                  type="button"
                  (click)="addProvider(openAi); connectionMenu.close()">
                  <span i18n="Name of the OpenAI provider">OpenAI</span>
                </button>
              }
              @if (!searchCredential.value()) {
                <button
                  app-menu-item
                  type="button"
                  (click)="addSearch(); connectionMenu.close()">
                  <span i18n="Name of the assistant web search connection">
                    Web search
                  </span>
                </button>
              }
            </app-dropdown-menu>
          }
        </div>
      </app-page-header>

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
          <div class="flex flex-col gap-7 pb-10">
            @if (banner(); as message) {
              <app-callout
                [color]="message.color"
                [icon]="message.icon"
                class="[&>div]:items-center">
                <div
                  class="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
                  <div class="min-w-0">
                    <p class="font-medium">{{ message.title }}</p>
                    <p class="text-muted mt-0.5">{{ message.body }}</p>
                  </div>
                  <button
                    app-flat-button
                    type="button"
                    class="h-8 shrink-0 px-3 text-xs"
                    (click)="actOnBanner(message)">
                    {{ message.action }}
                  </button>
                </div>
              </app-callout>
            }

            <app-assistant-status-band
              [enabled]="assistantEnabled()"
              [memberCount]="memberCount()"
              [spend]="spend.value()"
              [connected]="connectedCount()"
              [totalConnections]="totalConnections" />

            @if (canUpdateWorkspace()) {
              <div class="flex flex-col gap-4">
                <div class="flex items-center gap-3">
                  <span
                    class="text-muted text-[11px] font-bold tracking-[0.14em] uppercase"
                    i18n="Heading of the assistant setup group">
                    Setup
                  </span>
                  <span class="bg-foreground/10 h-px flex-1"></span>
                </div>

                <app-assistant-connections
                  #connections
                  [credentials]="credentials.value()"
                  [searchCredential]="searchCredential.value()"
                  (changed)="reloadConnections()" />

                <section
                  class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
                  <header class="border-border border-b px-6 py-5">
                    <h2
                      class="font-overpass text-base font-semibold"
                      i18n="Heading of the assistant access and privacy card">
                      Access &amp; privacy
                    </h2>
                  </header>

                  <div
                    class="border-border flex flex-wrap items-center justify-between gap-x-6 gap-y-3 border-b px-6 py-5">
                    <div class="min-w-0">
                      <h3
                        class="text-sm font-medium"
                        i18n="Heading of the assistant access setting">
                        Assistant access
                      </h3>
                      <p
                        class="text-muted mt-1 text-sm"
                        i18n="Explains what turning the assistant off does">
                        Turning this off stops new assistant messages and blocks
                        pending changes from being applied.
                      </p>
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
                    class="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 px-6 py-5">
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
                        When an import mapping is improved by the assistant, a
                        few real cell values are sent with the column names.
                        Turn this off to send column names and types only.
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
              </div>
            }

            <div class="flex flex-col gap-4">
              <div class="flex items-center gap-3">
                <span
                  class="text-muted text-[11px] font-bold tracking-[0.14em] uppercase"
                  i18n="Heading of the assistant activity group">
                  Activity
                </span>
                <span class="bg-foreground/10 h-px flex-1"></span>
              </div>

              @if (hasSpend()) {
                <app-assistant-spend-card
                  [spend]="spend.value()"
                  [canEditCap]="canUpdateWorkspace()"
                  (capChanged)="spend.reload()" />
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
                            {{
                              toDate(conversation.lastMessageAt) | prettyDate
                            }}
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
                            Explains why the assistant conversation list is
                            empty
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
          </div>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class WorkspaceAssistantViewComponent {
  protected readonly totalConnections = 3;
  protected readonly anthropic = AiProvider.anthropic;
  protected readonly openAi = AiProvider.openAi;

  private readonly http = inject(HttpClient);
  private readonly workspaceCommands = inject(WorkspaceCommandsService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly workspace = this.currentWorkspace.workspace;
  private readonly workspaceIdentifier = this.currentWorkspace.slug;
  private readonly members = workspaceUsersResource();

  private readonly connectionsCard = viewChild(AssistantConnectionsComponent);
  private readonly spendCard = viewChild(AssistantSpendCardComponent);

  protected readonly conversations = aiWorkspaceConversationResource();
  protected readonly credentials = aiCredentialResource(() => 'workspace');
  protected readonly searchCredential = searchCredentialResource();
  protected readonly spend = aiSpendResource();

  protected readonly selected = signal<AiConversationDetail | null>(null);
  protected readonly selectedMember = signal<string | null>(null);

  protected readonly conversationIcon = LucideMessagesSquare;
  protected readonly skeletonRows = Array.from({ length: 4 });

  protected readonly canUpdateWorkspace = hasPermission(
    PERMISSIONS.workspace.update
  );

  protected readonly canReadAudit = hasPermission(PERMISSIONS.audit.read);

  protected readonly workspaceKey = computed(() => {
    return this.workspaceIdentifier() ?? null;
  });

  protected readonly auditLink = computed(() => {
    return ['/', this.workspaceIdentifier() ?? '', 'audit'];
  });

  protected readonly memberCount = computed(() => this.members().length);

  protected readonly anthropicCredential = computed(() => {
    return this.credentialFor(AiProvider.anthropic);
  });

  protected readonly openAiCredential = computed(() => {
    return this.credentialFor(AiProvider.openAi);
  });

  protected readonly connectedCount = computed(() => {
    const providers = this.credentials.value().length;
    const search = this.searchCredential.value() ? 1 : 0;

    return providers + search;
  });

  protected readonly canAddConnection = computed(() => {
    const isMissingConnection = this.connectedCount() < this.totalConnections;

    return this.canUpdateWorkspace() && !this.selected() && isMissingConnection;
  });

  protected readonly assistantEnabled = computed(() => {
    return this.workspace()?.assistantEnabled !== false;
  });

  protected readonly allowsDataSampling = computed(() => {
    return this.workspace()?.allowAssistantDataSampling !== false;
  });

  protected readonly conversationCount = computed(() => {
    return this.conversations.value()?.length ?? 0;
  });

  protected readonly hasSpend = computed(() => !!this.spend.value());

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

  protected readonly banner = computed<AssistantBanner | null>(() => {
    if (!this.canUpdateWorkspace()) {
      return null;
    }

    const hasProviderKey = this.credentials.value().length > 0;

    if (!hasProviderKey) {
      return {
        icon: LucideKeyRound,
        color: 'primary',
        title: $localize`:Heading shown when no provider key is stored:No provider key yet`,
        body: $localize`:Explains why a workspace key helps:Add a workspace key so every member can use the assistant without bringing their own.`,
        action: $localize`:Button that adds a provider key:Add a key`,
        kind: 'add-key',
      };
    }

    if (!this.assistantEnabled()) {
      return {
        icon: LucidePower,
        color: 'warn',
        title: $localize`:Heading shown when the assistant is off:The assistant is turned off`,
        body: $localize`:Explains what the assistant being off means:Members cannot send new messages and pending changes will not apply. Keys and spend history are kept.`,
        action: $localize`:Button that turns the assistant back on:Turn back on`,
        kind: 'turn-on',
      };
    }

    return this.capBanner();
  });

  protected actOnBanner(banner: AssistantBanner) {
    if (banner.kind === 'turn-on') {
      this.setAssistantEnabled(true);

      return;
    }

    if (banner.kind === 'edit-cap') {
      this.spendCard()?.editCap();

      return;
    }

    this.addProvider(AiProvider.anthropic);
  }

  protected addProvider(provider: AiProvider) {
    this.connectionsCard()?.openProvider(provider);
  }

  protected addSearch() {
    this.connectionsCard()?.openSearch();
  }

  protected reloadConnections() {
    this.credentials.reload();
    this.searchCredential.reload();
  }

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

  private capBanner(): AssistantBanner | null {
    const spend = this.spend.value();
    const cap = spend?.cap;

    if (!spend || !cap) {
      return null;
    }

    const percent = Math.round((spend.monthToDate / cap) * 100);

    if (percent < 90) {
      return null;
    }

    const remaining = formatCurrency(Math.max(0, cap - spend.monthToDate));

    return {
      icon: LucideTriangleAlert,
      color: 'warn',
      title: $localize`:Heading warning that the spend cap is nearly used up:${percent}:percent:% of the monthly cap used`,
      body: $localize`:Explains what happens when the assistant spend cap is reached:${remaining}:remaining: left of ${formatCurrency(cap)}:cap:. The assistant stops accepting new messages once the cap is reached.`,
      action: $localize`:Button that changes the assistant spend cap:Edit cap`,
      kind: 'edit-cap',
    };
  }

  private credentialFor(provider: AiProvider) {
    return this.credentials
      .value()
      .find((credential) => credential.provider === provider);
  }
}
