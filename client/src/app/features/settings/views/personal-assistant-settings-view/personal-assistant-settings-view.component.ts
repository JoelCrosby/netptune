import { Component } from '@angular/core';
import { AiCredentialsComponent } from '@settings/components/ai-credentials/ai-credentials.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-personal-assistant-settings-view',
  imports: [
    AiCredentialsComponent,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the personal assistant key settings"
        title="Assistant" />

      <app-ai-credentials />
    </app-page-container>
  `,
})
export class PersonalAssistantSettingsViewComponent {}
