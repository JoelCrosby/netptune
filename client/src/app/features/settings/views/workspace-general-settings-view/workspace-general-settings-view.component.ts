import { Component } from '@angular/core';
import { WorkspaceDetailsComponent } from '@settings/components/workspace-details/workspace-details.component';
import { WorkspaceSettings } from '@settings/components/workspace-settings/workspace-settings.component';

@Component({
  selector: 'app-workspace-general-settings-view',
  imports: [WorkspaceDetailsComponent, WorkspaceSettings],
  template: `<app-workspace-details />

    <div class="border-border my-8 border-b-2"></div>

    <app-workspace-settings />`,
})
export class WorkspaceGeneralSettingsViewComponent {}
