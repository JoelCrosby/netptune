import { Component, effect, inject, untracked } from '@angular/core';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceListComponent } from '@app/features/workspaces/components/workspace-list.component';
import { BuildNumberComponent } from '@app/static/components/build-number/build-number.component';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  selector: 'app-workspaces-view',
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    WorkspaceListComponent,
    BuildNumberComponent,
  ],
  template: `
    <app-page-container [marginBottom]="true" [centerPage]="true">
      <app-page-header
        i18n-title="Page title for the workspace picker"
        title="Workspaces"
        i18n-actionTitle="Button that opens the create-workspace dialog"
        actionTitle="Create Workspace"
        (actionClick)="openWorkspaceDialog()" />

      @if (loading() && !loaded()) {
        <app-page-loading />
      } @else if (loadError() && !loaded()) {
        <app-error-state
          i18n-title="Shown when the workspace list fails to load"
          title="Your workspaces could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="reload()" />
      } @else {
        <app-workspace-list />
        <app-build-number />
      }
    </app-page-container>
  `,
})
export class WorkspacesViewComponent {
  private dialog = inject(DialogService);

  private list = inject(WorkspaceListService);

  loading = this.list.loading;
  loadError = this.list.loadError;
  protected loaded = this.list.loaded;
  private workspaces = this.list.workspaces;
  private initialSetupOpened = false;

  constructor() {
    effect(() => {
      if (
        !this.loaded() ||
        this.workspaces().length > 0 ||
        this.initialSetupOpened
      ) {
        return;
      }

      this.initialSetupOpened = true;
      untracked(() => this.openWorkspaceDialog());
    });
  }

  reload() {
    this.list.reload();
  }

  openWorkspaceDialog() {
    this.dialog.openWizard(WorkspaceDialogComponent, {
      title: $localize`:Title of a dialog or section:Create Workspace`,
      data: null,
      width: '720px',
    });
  }
}
