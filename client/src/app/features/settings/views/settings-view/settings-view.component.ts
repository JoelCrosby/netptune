import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SettingsComponent } from '@settings/components/settings/settings.component';
import { TagsComponent } from '@settings/components/tags/tags.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { WorkspaceSettings } from '@settings/components/workspace-settings/workspace-settings.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SettingsComponent,
    TagsComponent,
    WorkspaceSettings,
  ],
  template: `<app-page-container [centerPage]="true" [marginBottom]="true">
    <app-page-header title="Settings" />

    <app-settings />

    <div class="border-border my-8 border-b-2"></div>

    <app-tags />

    <div class="border-border my-8 border-b-2"></div>

    <app-workspace-settings />
  </app-page-container> `,
})
export class SettingsViewComponent {}
