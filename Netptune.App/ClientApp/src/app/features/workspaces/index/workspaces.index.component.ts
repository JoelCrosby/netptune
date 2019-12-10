import { TextHelpers } from '@core/util/text-helpers';
import { ConfirmDialogComponent } from '@app/shared/dialogs/confirm-dialog/confirm-dialog.component';
import { Component, OnInit } from '@angular/core';
import { dropIn } from '@core/animations/animations';
import { Workspace } from '@core/models/workspace';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';
import { MatDialog } from '@angular/material';
import { WorkspaceDialogComponent } from '@app/shared/dialogs/workspace-dialog/workspace-dialog.component';
import { Router } from '@angular/router';
import { selectAllWorkspaces } from '@core/workspaces/workspaces.selectors';
import {
  deleteWorkspace,
  loadWorkspaces,
} from '@core/workspaces/workspaces.actions';
import { ActionSelectWorkspace } from '@core/state/core.actions';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  animations: [dropIn],
})
export class WorkspacesComponent implements OnInit {
  workspaces$ = this.store.select(selectAllWorkspaces);

  constructor(
    private store: Store<AppState>,
    private dialog: MatDialog,
    private router: Router
  ) {}

  ngOnInit() {
    this.store.dispatch(loadWorkspaces());
  }

  trackById(index: number, workspace: Workspace) {
    return workspace.id;
  }

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent);
  }

  goToProjectsClicked(workspace: Workspace) {
    this.store.dispatch(new ActionSelectWorkspace(workspace));
  }

  deleteClicked(workspace: Workspace) {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Workspace',
          content: `Are you sure you want to delete ${TextHelpers.truncate(
            workspace.name
          )}`,
          confirm: 'Delete',
        },
      })
      .afterClosed()
      .subscribe(result => {
        if (result) {
          this.store.dispatch(deleteWorkspace({ workspace }));
        }
      });
  }
}
