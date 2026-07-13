import { Component, inject } from '@angular/core';
import { WorkspaceListComponent } from '@app/features/workspaces/components/workspace-list.component';
import { BuildNumberComponent } from '@app/static/components/build-number/build-number.component';
import { DialogService } from '@core/services/dialog.service';
import { selectWorkspacesLoading } from '@core/store/workspaces/workspaces.selectors';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    WorkspaceListComponent,
    BuildNumberComponent,
  ],
  template: `<app-page-container [marginBottom]="true" [centerPage]="true">
    <app-page-header
      title="Workspaces"
      actionTitle="Create Workspace"
      (actionClick)="openWorkspaceDialog()" />

    @if (loading()) {
      <app-page-loading />
    } @else {
      <app-workspace-list />
      <app-build-number />
    }
  </app-page-container> `,
})
export class WorkspacesViewComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectWorkspacesLoading);

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '720px',
    });
  }
}
