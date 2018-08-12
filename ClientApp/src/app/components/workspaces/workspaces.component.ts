import { Component, OnInit } from '@angular/core';
import { Workspace } from '../../models/workspace';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { AlertService } from '../../services/alert/alert.service';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';

@Component({
  selector: 'app-workspaces',
  templateUrl: './workspaces.component.html',
  styleUrls: ['./workspaces.component.scss']
})
export class WorkspacesComponent implements OnInit {

  public workspaces: Workspace[];

  public inputName: string;
  public inputDescription: string;

  closeResult: string;
  selectedWorkspace: Workspace;

  constructor(
    public workspaceService: WorkspaceService,
    private alertsService: AlertService,
    private router: Router,
    private modalService: NgbModal) { }

  ngOnInit() {
    this.workspaceService.currentWorkspace = null;
    this.getWorkspaces();
  }

  trackById(index: number, workspace: Workspace) {
    return workspace.workspaceId;
  }

  showAddModal(content): void {
    this.open(content);
  }

  goToProjectsClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['projects']);
  }

  manageUsersClicked(workspace: Workspace): void {
    this.workspaceService.currentWorkspace = workspace;
    this.router.navigate(['users']);
  }

  open(content) {
    this.modalService.open(content, { centered: true, size: 'lg', windowClass: 'modal-floating' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      if (this.selectedWorkspace) {
        const newProject = new Workspace();
        newProject.workspaceId = this.selectedWorkspace.workspaceId;
        newProject.name = this.inputName;
        newProject.description = this.inputDescription;
        this.updateWorkspace(newProject);
      } else {
        const newProject = new Workspace();
        newProject.name = this.inputName;
        newProject.description = this.inputDescription;
        this.addWorkspace(newProject);
      }

      this.clearModalValues();

    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
      this.clearModalValues();
    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedWorkspace = null;

    // clear modal fields.
    this.inputName = null;
    this.inputDescription = null;
  }

  getWorkspaces(): void {
    this.workspaceService.getWorkspaces()
      .subscribe(workspaces => this.workspaces = workspaces);
  }

  addWorkspace(workspace: Workspace): void {
    this.workspaceService.addWorkspace(workspace)
      .subscribe((projectResult) => {
        if (projectResult) {
          this.getWorkspaces();
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
        console.log(result);
        workspace = result;
        this.getWorkspaces();
        this.alertsService.changeSuccessMessage('Workspace updated!');
      });
  }

  private getDismissReason(reason: any): string {
    if (reason === ModalDismissReasons.ESC) {
      return 'by pressing ESC';
    } else if (reason === ModalDismissReasons.BACKDROP_CLICK) {
      return 'by clicking on a backdrop';
    } else {
      return `with: ${reason}`;
    }
  }

}
