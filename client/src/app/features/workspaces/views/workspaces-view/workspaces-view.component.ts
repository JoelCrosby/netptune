import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { loadBuildInfo } from '@core/store/meta/meta.actions';
import { selectBuildInfo } from '@core/store/meta/meta.selectors';
import { selectWorkspacesLoading } from '@core/store/workspaces/workspaces.selectors';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';

@Component({
  templateUrl: './workspaces-view.component.html',
  styleUrls: ['./workspaces-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspacesViewComponent implements OnInit {
  buildInfo$ = this.store.select(selectBuildInfo);
  loading$ = this.store.select(selectWorkspacesLoading);

  constructor(private dialog: MatDialog, private store: Store<AppState>) {}

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
