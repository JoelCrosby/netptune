import { Component, OnInit } from '@angular/core';
import { dropIn } from '@app/core/animations/animations';
import { Workspace } from '@app/core/models/workspace';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import { MatDialog } from '@angular/material';
import { WorkspaceDialogComponent } from '@app/shared/dialogs/workspace-dialog/workspace-dialog.component';
import { Router } from '@angular/router';
import { selectWorkspaces } from '../store/workspaces.selectors';
import { ActionLoadWorkspaces } from '../store/workspaces.actions';
import { ActionSelectWorkspace } from '@app/core/state/core.actions';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  animations: [dropIn],
})
export class WorkspacesComponent implements OnInit {
  workspaces$ = this.store.select(selectWorkspaces);

  constructor(private store: Store<AppState>, private dialog: MatDialog, private router: Router) {}

  ngOnInit() {
    this.store.dispatch(new ActionLoadWorkspaces());
  }

  trackById(index: number, workspace: Workspace) {
    return workspace.id;
  }

  openWorkspaceDialog() {
    this.dialog.open<WorkspaceDialogComponent>(WorkspaceDialogComponent);
  }

  goToProjectsClicked(workspace: Workspace) {
    this.store.dispatch(new ActionSelectWorkspace(workspace));
    this.router.navigate(['/projects']);
  }
}
