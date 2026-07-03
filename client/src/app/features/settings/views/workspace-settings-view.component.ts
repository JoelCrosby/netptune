import { Component, inject } from '@angular/core';
import { StatusesComponent } from '@settings/components/statuses/statuses.component';
import { TagsComponent } from '@settings/components/tags/tags.component';
import { WorkspaceDetailsComponent } from '@settings/components/workspace-details/workspace-details.component';
import { WorkspaceSettings } from '@settings/components/workspace-settings/workspace-settings.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { Store } from '@ngrx/store';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    StatusesComponent,
    TagsComponent,
    WorkspaceDetailsComponent,
    WorkspaceSettings,
  ],
  template: `<app-page-container [centerPage]="true" [marginBottom]="true">
    <app-page-header title="Workspace" />

    @if (readWorkspace()) {
      <app-workspace-details />
    }

    @if (readTags()) {
      <div class="border-border my-8 border-b-2"></div>
      <app-tags />
    }

    @if (readStatuses()) {
      <div class="border-border my-8 border-b-2"></div>
      <app-statuses />
    }

    @if (readWorkspace()) {
      <div class="border-border my-8 border-b-2"></div>
      <app-workspace-settings />
    }
  </app-page-container> `,
})
export class WorkspaceSettingsViewComponent {
  readonly store = inject(Store);

  readWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.read)
  );

  readStatuses = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );
}
