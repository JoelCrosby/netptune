import { Component, OnInit } from '@angular/core';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';
import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects/projects.service';
import { AlertService } from '../../services/alert/alert.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { ProjectType } from '../../models/project-type';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.scss']
})
export class ProjectsComponent implements OnInit {

  public projects: Project[];
  public projectTypes: ProjectType[];

  public showAddForm = false;

  public inputName: string;
  public inputDescription: string;
  public inputType: number;

  closeResult: string;
  selectedProject: Project;

  constructor(
    private projectsService: ProjectsService,
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private modalService: NgbModal) {

  }

  ngOnInit(): void {
    this.getProjects();
    this.getProjectTypes();
  }

  trackById(index: number, project: Project) {
    return project.projectId;
  }

  getProjects(): void {
    this.projectsService.getProjects()
    .subscribe(projects => this.projects = projects);
  }

  getProjectTypes(): void {
    this.projectTypeService.getProjectTypes()
    .subscribe(projectTypes => this.projectTypes = projectTypes);
  }

  addProject(project: Project): void {
    this.projectsService.addProject(project)
        .subscribe((projectResult) => {
          if (projectResult) {
            this.getProjects();
            this.alertsService.changeSuccessMessage('Project added!');
          }
      }, error => {
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Project. ' + error);
      });
  }

  updateProject(project: Project): void {
    this.projectsService.updateProject(project)
      .subscribe( result => {
        project = result;
        this.alertsService.changeSuccessMessage('Project updated!');
      });
  }

  deleteProject(project: Project): void {
    this.projectsService.deleteProject(project)
      .subscribe( projectResult => {
          this.projects.forEach( (item, itemIndex) => {
            if (item.projectId === projectResult.projectId) {
              this.projects.splice(itemIndex, 1);
              this.alertsService.changeSuccessMessage('Project deleted!');
            }
          });
        }
      );
  }

  showAddModal(content): void {
    this.open(content);
  }

  showUpdateModal(content, project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.inputName = project.name;
    this.inputDescription = project.description;
    this.inputType = project.projectType.projectTypeId;

    this.open(content);
  }

  showDeleteModal(confirm, project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.inputName = project.name;
    this.inputDescription = project.description;
    this.inputType = project.projectType.projectTypeId;

    this.openConfirmationDialog(confirm, this.selectedProject);
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedProject = null;

    // clear modal fields.
    this.inputName = null;
    this.inputDescription = null;
    this.inputType = null;
  }

  open(content) {
    this.modalService.open(content, { centered: true, size: 'lg', windowClass: 'modal-floating' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      if (this.selectedProject) {
        const newProject = new Project();
        newProject.projectId = this.selectedProject.projectId;
        newProject.name = this.inputName;
        newProject.description = this.inputDescription;
        newProject.projectType = this.inputType;
        this.updateProject(newProject);
      } else {
        const newProject = new Project();
        newProject.name = this.inputName;
        newProject.description = this.inputDescription;
        newProject.projectType = this.inputType;

        this.addProject(newProject);
      }

      this.clearModalValues();

    }, (reason) => {
      this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
      this.clearModalValues();
    });
  }

  openConfirmationDialog(content, project: Project) {
    this.modalService.open(content, { centered: true, size: 'lg' }).result.then((result) => {
      this.closeResult = `Closed with: ${result}`;

      if (!project) {
        return;
      } else {
        this.deleteProject(project);
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

}
