import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { DialogService } from '@core/services/dialog.service';
import { loadBuildInfo } from '@core/store/meta/meta.actions';
import { selectBuildInfo } from '@core/store/meta/meta.selectors';
import { selectWorkspacesLoading } from '@core/store/workspaces/workspaces.selectors';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { WorkspaceListComponent } from '@workspaces/components/workspace-list/workspace-list.component';

@Component({
  templateUrl: './workspaces-view.component.html',
  styleUrls: ['./workspaces-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    WorkspaceListComponent,
  ],
})
export class WorkspacesViewComponent implements OnInit {
  private dialog = inject(DialogService);
  private store = inject(Store);

  buildInfo = this.store.selectSignal(selectBuildInfo);
  loading = this.store.selectSignal(selectWorkspacesLoading);

  ngOnInit() {
    this.store.dispatch(loadBuildInfo());
  }

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '720px',
    });
  }
}
