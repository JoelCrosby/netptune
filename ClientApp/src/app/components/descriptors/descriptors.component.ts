import { Component, OnInit } from '@angular/core';
import { ProjectType } from '../../models/project-type';
import { AlertService } from '../../services/alert/alert.service';
import { Router } from '../../../../node_modules/@angular/router';
import { NgbModal, ModalDismissReasons } from '../../../../node_modules/@ng-bootstrap/ng-bootstrap';
import { ProjectTypeService } from '../../services/project-type/project-type.service';

@Component({
  selector: 'app-descriptors',
  templateUrl: './descriptors.component.html',
  styleUrls: ['./descriptors.component.scss']
})
export class DescriptorsComponent implements OnInit {

  public inputName: string;
  public inputDescription: string;

  closeResult: string;
  selectedProjectType: ProjectType;

  constructor(
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private router: Router,
    private modalService: NgbModal) { }

  ngOnInit() {
    this.getProjectTypes();
  }

  trackById(index: number, projectType: ProjectType) {
    return projectType.id;
  }

  showAddModal(content): void {
    this.open(content);
  }

  open(content) {
    this.modalService.open(content, { centered: true, size: 'lg', windowClass: 'modal-floating' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      if (this.selectedProjectType) {
        const newProjectType = new ProjectType();
        newProjectType.id = this.selectedProjectType.id;
        newProjectType.name = this.inputName;
        newProjectType.description = this.inputDescription;
        this.updateProjectType(newProjectType);
      } else {
        const newProjectType = new ProjectType();
        newProjectType.name = this.inputName;
        newProjectType.description = this.inputDescription;
        this.addProjectType(newProjectType);
      }

      this.clearModalValues();

    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
      this.clearModalValues();
    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedProjectType = null;

    // clear modal fields.
    this.inputName = null;
    this.inputDescription = null;
  }

  getProjectTypes(): void {
    this.projectTypeService.refreshProjectTypes();
  }

  addProjectType(projectType: ProjectType): void {
    this.projectTypeService.addProjectType(projectType)
      .subscribe((projectTypeResult) => {
        if (projectTypeResult) {
          this.getProjectTypes();
          this.alertsService.changeSuccessMessage('Workspace added!');
        }
      }, error => {
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Workspace. ' + error);
      });
  }

  updateProjectType(projectType: ProjectType): void {
    this.projectTypeService.updateProjectType(projectType)
      .subscribe(result => {
        projectType = result;
        this.getProjectTypes();
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
