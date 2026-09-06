import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiConversationDetail } from '@core/models/ai-conversation';
import { AiProvider } from '@core/models/ai-credential';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { ClientResponse } from '@core/models/client-response';
import { aiCredentialResource } from '@core/resources/ai-credential.resource';
import { aiSpendResource } from '@core/resources/ai-spend.resource';
import { searchCredentialResource } from '@core/resources/search-credential.resource';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import {
  AssistantBannerAction,
  AssistantBannerComponent,
} from '@settings/components/assistant/assistant-banner.component';
import { AssistantConnectionsComponent } from '@settings/components/assistant/assistant-connections.component';
import { AssistantConversationsCardComponent } from '@settings/components/assistant/assistant-conversations-card.component';
import { AssistantHeaderActionsComponent } from '@settings/components/assistant/assistant-header-actions.component';
import { AssistantPrivacyCardComponent } from '@settings/components/assistant/assistant-privacy-card.component';
import { AssistantSectionHeadingComponent } from '@settings/components/assistant/assistant-section-heading.component';
import { AssistantSpendCardComponent } from '@settings/components/assistant/assistant-spend-card.component';
import { AssistantStatusBandComponent } from '@settings/components/assistant/assistant-status-band.component';
import { AssistantTranscriptComponent } from '@settings/components/assistant/assistant-transcript.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-workspace-assistant-view',
  imports: [
    AssistantBannerComponent,
    AssistantConnectionsComponent,
    AssistantConversationsCardComponent,
    AssistantHeaderActionsComponent,
    AssistantPrivacyCardComponent,
    AssistantSectionHeadingComponent,
    AssistantSpendCardComponent,
    AssistantStatusBandComponent,
    AssistantTranscriptComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the workspace assistant settings"
        title="Assistant">
        <app-assistant-header-actions
          pageHeaderActions
          [credentials]="credentials.value()"
          [searchCredential]="searchCredential.value()"
          [canAddConnection]="canAddConnection()"
          (addProvider)="addProvider($event)"
          (addSearch)="addSearch()" />
      </app-page-header>

      <app-page-body scroll>
        @if (selected(); as detail) {
          <app-assistant-transcript
            [detail]="detail"
            [member]="selectedMember()"
            [workspace]="workspaceKey()"
            (closed)="clearSelection()" />
        }
        <!-- Kept mounted while a transcript is open so the table holds its
             page and sort when the reader comes back. -->
        <div class="flex flex-col gap-7 pb-10" [hidden]="selected()">
          <app-assistant-banner
            [canUpdateWorkspace]="canUpdateWorkspace()"
            [hasProviderKey]="hasProviderKey()"
            [enabled]="assistantEnabled()"
            [spend]="spend.value()"
            (action)="actOnBanner($event)" />

          <app-assistant-status-band
            [enabled]="assistantEnabled()"
            [memberCount]="memberCount()"
            [spend]="spend.value()"
            [connected]="connectedCount()"
            [totalConnections]="totalConnections" />

          @if (canUpdateWorkspace()) {
            <div class="flex flex-col gap-4">
              <app-assistant-section-heading
                i18n-label="Heading of the assistant setup group"
                label="Setup" />

              <app-assistant-connections
                [credentials]="credentials.value()"
                [searchCredential]="searchCredential.value()"
                (changed)="reloadConnections()" />

              <app-assistant-privacy-card
                [enabled]="assistantEnabled()"
                [dataSampling]="allowsDataSampling()"
                (enabledChanged)="setAssistantEnabled($event)"
                (dataSamplingChanged)="setAllowDataSampling($event)" />
            </div>
          }

          <div class="flex flex-col gap-4">
            <app-assistant-section-heading
              i18n-label="Heading of the assistant activity group"
              label="Activity" />

            @if (hasSpend()) {
              <app-assistant-spend-card
                [spend]="spend.value()"
                [canEditCap]="canUpdateWorkspace()"
                (capChanged)="spend.reload()" />
            }

            <app-assistant-conversations-card (opened)="select($event)" />
          </div>
        </div>
      </app-page-body>
    </app-page-container>
  `,
})
export class WorkspaceAssistantViewComponent {
  protected readonly totalConnections = 3;

  private readonly http = inject(HttpClient);
  private readonly workspaceCommands = inject(WorkspaceCommandsService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly workspace = this.currentWorkspace.workspace;
  private readonly members = workspaceUsersResource();

  private readonly connectionsCard = viewChild(AssistantConnectionsComponent);
  private readonly spendCard = viewChild(AssistantSpendCardComponent);

  protected readonly credentials = aiCredentialResource(() => 'workspace');
  protected readonly searchCredential = searchCredentialResource();
  protected readonly spend = aiSpendResource();

  protected readonly selected = signal<AiConversationDetail | null>(null);
  protected readonly selectedMember = signal<string | null>(null);

  protected readonly canUpdateWorkspace = hasPermission(
    PERMISSIONS.workspace.update
  );

  protected readonly workspaceKey = computed(() => {
    return this.currentWorkspace.slug() ?? null;
  });

  protected readonly memberCount = computed(() => this.members().length);

  protected readonly hasProviderKey = computed(() => {
    return this.credentials.value().length > 0;
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

  protected readonly hasSpend = computed(() => !!this.spend.value());

  protected actOnBanner(action: AssistantBannerAction) {
    if (action === 'turn-on') {
      this.setAssistantEnabled(true);

      return;
    }

    if (action === 'edit-cap') {
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

  protected select(conversation: AiWorkspaceConversation) {
    this.selectedMember.set(conversation.userDisplayName);

    this.http
      .get<ClientResponse<AiConversationDetail>>(
        `api/ai/admin/conversations/${conversation.id}`
      )
      .subscribe((response) => this.selected.set(response.payload ?? null));
  }

  protected clearSelection() {
    this.selected.set(null);
    this.selectedMember.set(null);
  }
}
