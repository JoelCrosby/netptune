import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { dropIn } from '@core/animations/animations';
import { AppState } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { TextHelpers } from '@core/util/text-helpers';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [dropIn],
})
export class WorkspacesComponent implements OnInit {
  workspaces$ = this.store.select(selectAllWorkspaces);

  constructor(private store: Store<AppState>, private dialog: MatDialog) {}

  ngOnInit() {
    this.store.dispatch(WorkspaceActions.loadWorkspaces());
  }

  trackById(index: number, workspace: Workspace) {
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
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '600px',
        data: {
          title: 'Delete Workspace',
          content: `Are you sure you want to delete ${TextHelpers.truncate(
            workspace.name
          )}`,
          confirm: 'Delete',
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result) {
          this.store.dispatch(WorkspaceActions.deleteWorkspace({ workspace }));
        }
      });
  }
}
