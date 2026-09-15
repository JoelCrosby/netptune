import { Component, computed, viewChild } from '@angular/core';
import { AiProvider } from '@core/models/ai-credential';
import {
  aiCredentialAvailabilityResource,
  aiCredentialResource,
} from '@core/resources/ai-credential.resource';
import { AssistantConnectionsComponent } from '@settings/components/assistant/assistant-connections.component';
import { AssistantOwnConversationsCardComponent } from '@settings/components/assistant/assistant-own-conversations-card.component';
import { AssistantHeaderActionsComponent } from '@settings/components/assistant/assistant-header-actions.component';
import { AssistantSectionHeadingComponent } from '@settings/components/assistant/assistant-section-heading.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-personal-assistant-settings-view',
  imports: [
    AssistantConnectionsComponent,
    AssistantHeaderActionsComponent,
    AssistantOwnConversationsCardComponent,
    AssistantSectionHeadingComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the personal assistant key settings"
        title="Assistant">
        <app-assistant-header-actions
          pageHeaderActions
          scope="user"
          [credentials]="credentials.value()"
          [canAddConnection]="canAddConnection()"
          (addProvider)="addProvider($event)" />
      </app-page-header>

      <app-page-body scroll>
        <div class="flex flex-col gap-7 pb-10">
          <div class="flex flex-col gap-4">
            <app-assistant-section-heading
              i18n-label="Heading of the assistant setup group"
              label="Setup" />

            <app-assistant-connections
              scope="user"
              [credentials]="credentials.value()"
              [availability]="availability.value()"
              (changed)="reloadConnections()" />
          </div>

          <div class="flex flex-col gap-4">
            <app-assistant-section-heading
              i18n-label="Heading of the assistant activity group"
              label="Activity" />

            <app-assistant-own-conversations-card />
          </div>
        </div>
      </app-page-body>
    </app-page-container>
  `,
})
export class PersonalAssistantSettingsViewComponent {
  private readonly totalConnections = 2;

  private readonly connectionsCard = viewChild(AssistantConnectionsComponent);

  protected readonly credentials = aiCredentialResource(() => 'user');
  protected readonly availability = aiCredentialAvailabilityResource();

  protected readonly canAddConnection = computed(() => {
    return this.credentials.value().length < this.totalConnections;
  });

  protected addProvider(provider: AiProvider) {
    this.connectionsCard()?.openProvider(provider);
  }

  protected reloadConnections() {
    this.credentials.reload();
    this.availability.reload();
  }
}
