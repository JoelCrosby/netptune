import { Component } from '@angular/core';
import { WorkspaceDetailsComponent } from '@settings/components/workspace-details/workspace-details.component';
import { WorkspaceSettings } from '@settings/components/workspace-settings/workspace-settings.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-workspace-general-settings-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    WorkspaceDetailsComponent,
    WorkspaceSettings,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for general workspace settings"
        title="General" />

      <app-workspace-details />

      <div class="border-border my-8 border-b-2"></div>

      <app-workspace-settings />
    </app-page-container>
  `,
})
export class WorkspaceGeneralSettingsViewComponent {}
