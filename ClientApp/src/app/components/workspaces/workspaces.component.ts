import { Component, OnInit } from '@angular/core';
import { Workspace } from '../../models/workspace';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { AlertService } from '../../services/alert/alert.service';
import { Router } from '@angular/router';

import { WorkspaceDialogComponent } from '../dialogs/workspace-dialog/workspace-dialog.component';
import { MatDialog, MatSnackBar } from '@angular/material';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { dropIn } from '../../animations';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.component.html',
  styleUrls: ['./workspaces.component.scss'],
  animations: [dropIn]
})
export class WorkspacesComponent implements OnInit {

  selectedWorkspace: Workspace;

  constructor(
    public workspaceService: WorkspaceService,
    private alertsService: AlertService,
    private router: Router,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) { }

  ngOnInit() {
    this.workspaceService.currentWorkspace = null;
    this.workspaceService.refreshWorkspaces();
  }

  trackById(index: number, workspace: Workspace) {
    return workspace.workspaceId;
  }

  showAddModal(): void {
    this.open();
  }

  goToProjectsClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['projects']);
  }

  manageUsersClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['users']);
  }

  open() {

    const dialogRef = this.dialog.open(WorkspaceDialogComponent, {
      width: '500px',
      data: this.selectedWorkspace
    });

    dialogRef.afterClosed().subscribe((result: Workspace) => {

      if (!result) { return; }

      if (this.selectedWorkspace) {
        const newProject = new Workspace();
        newProject.workspaceId = this.selectedWorkspace.workspaceId;
        newProject.name = result.name;
        newProject.description = result.description;
        this.updateWorkspace(newProject);
      } else {
        const newProject = new Workspace();
        newProject.name = result.name;
        newProject.description = result.description;
        this.addWorkspace(newProject);
      }

      this.clearModalValues();
    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedWorkspace = null;
  }

  addWorkspace(workspace: Workspace): void {
    this.workspaceService.addWorkspace(workspace)
      .subscribe((projectResult) => {
        if (projectResult) {
          this.workspaceService.refreshWorkspaces();
          this.alertsService.changeSuccessMessage('Workspace added!');
        }
      }, error => {
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Workspace. ' + error);
      });
  }

  updateWorkspace(workspace: Workspace): void {
    this.workspaceService.updateWorkspace(workspace)
      .subscribe(result => {
        workspace = result;
        this.workspaceService.refreshWorkspaces();
        this.alertsService.changeSuccessMessage('Workspace updated!');
      });
  }

  deleteClicked(workspace: Workspace) {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '500px',
      data: {
        title: 'Remove Workspace',
        content: `Are you sure you wish to remove ${workspace.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: Boolean) => {

      if (result) {
        this.workspaceService.deleteWorkspace(workspace)
          .subscribe((data: Workspace) => {
            workspace = data;
            this.workspaceService.refreshWorkspaces();
            this.snackBar.open('Workspace Deleted.', 'Undo', {
              duration: 3000,
            });

          }, error => {
            this.snackBar.open('An error occured while trying to delete workspace' + error, null, {
              duration: 2000,
            });
          });
      }

      this.clearModalValues();
    });

  }

  inviteUsersClicked(workspace: Workspace): void {
  }

  exportDataClicked(workspace: Workspace): void {
  }

}
