import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Workspace } from '@core/models/workspace';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspacesComponent implements OnInit {
  workspaces$ = this.store.select(selectAllWorkspaces);

  constructor(private store: Store, private dialog: MatDialog) {}

  ngOnInit() {
    this.store.dispatch(WorkspaceActions.loadWorkspaces());
  }

  trackById(_: number, workspace: Workspace) {
    return workspace.id;
  }

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '600px',
    });
  }

  onEditClicked(workspace: Workspace) {
    this.dialog.open(WorkspaceDialogComponent, {
      data: workspace,
      width: '600px',
    });
  }

  deleteClicked(workspace: Workspace) {
    this.store.dispatch(WorkspaceActions.deleteWorkspace({ workspace }));
  }
}
