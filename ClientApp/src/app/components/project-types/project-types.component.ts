import { Component, OnInit } from '@angular/core';
import { ProjectType } from '../../models/project-type';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { AlertService } from '../../services/alert/alert.service';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-project-types',
  templateUrl: './project-types.component.html',
  styleUrls: ['./project-types.component.scss']
})
export class ProjectTypesComponent implements OnInit {

  public projectTypes: ProjectType[];

  public showAddForm = false;

  public inputName: string;
  public inputDescription: string;
  public inputTypeCode: string;

  closeResult: string;
  selectedProjectType: ProjectType;

  constructor(
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private workspaceService: WorkspaceService,
    private modalService: NgbModal) {

  }

  ngOnInit() {
    this.getProjectTypes();
  }

  trackById(index: number, project: ProjectType) {
    return project.id;
  }

  getProjectTypes(): void {
    this.projectTypeService.getProjectTypes()
      .subscribe(projectTypes => this.projectTypes = projectTypes);
  }

  showAddModal(content): void {
    this.open(content);
  }

  showUpdateModal(content, projectType: ProjectType): void {
    if (projectType == null) { return; }

    this.selectedProjectType = projectType;
    this.inputName = projectType.name;
    this.inputDescription = projectType.description;
    this.inputTypeCode = projectType.typeCode;

    this.open(content);
  }

  showDeleteModal(confirm, projectType: ProjectType): void {
    if (projectType == null) { return; }

    this.selectedProjectType = projectType;
    this.inputName = projectType.name;
    this.inputDescription = projectType.description;
    this.inputTypeCode = projectType.typeCode;

    this.openConfirmationDialog(confirm, this.selectedProjectType);
  }

  addProjectType(projectType: ProjectType): void {
    this.projectTypeService.addProjectType(projectType)
      .subscribe((projectTypeResult) => {
        if (projectTypeResult) {
          this.getProjectTypes();
          this.alertsService.changeSuccessMessage('Project added!');
        }
      }, error => {
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Project. ' + error);
      });
  }

  getFaIcon(projectType: ProjectType): string {

    switch (projectType.typeCode) {
      case 'node':
        return 'fab fa-node-js';
      case 'angular':
        return 'fab fa-angular';
      case 'winforms':
        return 'fab fa-windows';
      case 'aspcore':
        return 'fas fa-code';
      default:
        return 'fas fa-chalkboard';
    }
  }

  updateProjectType(projectType: ProjectType): void {
    this.projectTypeService.updateProjectType(projectType)
      .subscribe(result => {
        console.log(result);
        projectType = result;
        this.getProjectTypes();
        this.alertsService.changeSuccessMessage('Project updated!');
      });
  }

  deleteProjectType(projectType: ProjectType): void {
    this.projectTypeService.deleteProjectType(projectType)
      .subscribe(projectTypeResult => {
        this.projectTypes.forEach((item, itemIndex) => {
          if (item.id === projectTypeResult.id) {
            this.projectTypes.splice(itemIndex, 1);
            this.alertsService.changeSuccessMessage('Project deleted!');
          }
        });
      }
      );
  }

  open(content) {
    this.modalService.open(content, { centered: true, size: 'lg', windowClass: 'modal-floating' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      if (this.selectedProjectType) {
        const newProjectType = new ProjectType();
        newProjectType.id = this.selectedProjectType.id;
        newProjectType.name = this.inputName;
        newProjectType.description = this.inputDescription;
        newProjectType.typeCode = this.inputTypeCode;
        this.updateProjectType(newProjectType);
      } else {
        const newProjectType = new ProjectType();
        newProjectType.name = this.inputName;
        newProjectType.description = this.inputDescription;
        newProjectType.typeCode = this.inputTypeCode;
        this.addProjectType(newProjectType);
      }

      this.clearModalValues();

    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
      this.clearModalValues();
    });
  }

  openConfirmationDialog(content, projectType: ProjectType) {
    this.modalService.open(content, { centered: true, size: 'lg' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      console.log(projectType);

      if (!projectType) {
        return;
      } else {
        this.deleteProjectType(projectType);
      }

      this.clearModalValues();

    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
      this.clearModalValues();
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

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedProjectType = null;

    // clear modal fields.
    this.inputName = null;
    this.inputDescription = null;
    this.inputTypeCode = null;
  }

}
