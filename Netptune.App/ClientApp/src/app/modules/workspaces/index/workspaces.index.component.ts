import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { Router } from '@angular/router';
import { dropIn } from '../../../core/animations/animations';
import { ConfirmDialogComponent } from '../../../dialogs/confirm-dialog/confirm-dialog.component';
import { Workspace } from '../../../models/workspace';
import { UserService } from '../../../services/user/user.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';
import { WorkspaceDialogComponent } from '../../../dialogs/workspace-dialog/workspace-dialog.component';
import { Maybe } from '../../../core/types/nothing';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.index.component.html',
  styleUrls: ['./workspaces.index.component.scss'],
  animations: [dropIn]
})
export class WorkspacesComponent implements OnInit {

  selectedWorkspace: Maybe<Workspace>;

  constructor(
    public workspaceService: WorkspaceService,
    private userService: UserService,
    private router: Router,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) { }

  ngOnInit() {
    this.workspaceService.clearCurrentWorkspace();
    this.workspaceService.refreshWorkspaces();
  }

  trackById(index: number, workspace: Workspace) {
    return workspace.id;
  }

  showAddModal(): void {
    this.open();
  }

  goToProjectsClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['/projects']);
  }

  manageUsersClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['/users']);
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
        newProject.id = this.selectedWorkspace.id;
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

  async addWorkspace(workspace: Workspace) {
    try {
      const projectResult = await this.workspaceService.addWorkspace(workspace).toPromise();

      if (projectResult) {
        this.workspaceService.refreshWorkspaces();
      }
    } catch (error) {

    }
  }

  async updateWorkspace(workspace: Workspace) {
    const result = await this.workspaceService.updateWorkspace(workspace).toPromise();

    workspace = result;
    this.workspaceService.refreshWorkspaces();
  }

  async deleteClicked(workspace: Workspace) {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '500px',
      data: {
        title: 'Remove Workspace',
        content: `Are you sure you wish to remove ${workspace.name}?`,
        confirm: 'Remove'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();

    if (!result) {
      this.clearModalValues();
      return;
    }

    try {
      const data = await this.workspaceService.deleteWorkspace(workspace).toPromise();

      workspace = data;
      this.workspaceService.refreshWorkspaces();
      this.snackBar.open('Workspace Deleted.', 'Undo', {
        duration: 3000,
      });

    } catch (error) {
    }
    this.clearModalValues();
  }

  async inviteUsersClicked(): Promise<void> {
    await this.userService.showInviteUserDialog();
  }

  exportDataClicked(workspace: Workspace): void {
  }

}
