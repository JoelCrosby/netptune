import { Component } from '@angular/core';
import { StatusesComponent } from '@settings/components/statuses/statuses.component';
import { TagsComponent } from '@settings/components/tags/tags.component';
import { WorkspaceSettings } from '@settings/components/workspace-settings/workspace-settings.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    StatusesComponent,
    TagsComponent,
    WorkspaceSettings,
  ],
  template: `<app-page-container [centerPage]="true" [marginBottom]="true">
    <app-page-header title="Workspace" />

    <app-tags />

    <div class="border-border my-8 border-b-2"></div>

    <app-statuses />

    <div class="border-border my-8 border-b-2"></div>

    <app-workspace-settings />
  </app-page-container> `,
})
export class WorkspaceSettingsViewComponent {}
