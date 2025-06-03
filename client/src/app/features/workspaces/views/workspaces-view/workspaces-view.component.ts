import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { loadBuildInfo } from '@core/store/meta/meta.actions';
import { selectBuildInfo } from '@core/store/meta/meta.selectors';
import { selectWorkspacesLoading } from '@core/store/workspaces/workspaces.selectors';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';

@Component({
    templateUrl: './workspaces-view.component.html',
    styleUrls: ['./workspaces-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class WorkspacesViewComponent implements OnInit {
  buildInfo$ = this.store.select(selectBuildInfo);
  loading$ = this.store.select(selectWorkspacesLoading);

  constructor(
    private dialog: DialogService,
    private store: Store
  ) {}

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
