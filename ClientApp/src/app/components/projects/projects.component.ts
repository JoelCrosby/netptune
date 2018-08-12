import { Component, OnInit } from '@angular/core';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';
import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects/projects.service';
import { AlertService } from '../../services/alert/alert.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { ProjectType } from '../../models/project-type';
import { FileSave } from '../../../../node_modules/file-saver/FileSaver';

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

  public exportInProgress = false;

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

  getProjectTypeName(project: Project): string {
    if (!project.projectTypeId || !this.projectTypes) { return; }
    return this.projectTypes.filter(item => item.projectTypeId === project.projectTypeId)[0].name;
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
      .subscribe(result => {
        console.log(result);
        project = result;
        this.getProjects();
        this.alertsService.changeSuccessMessage('Project updated!');
      });
  }

  deleteProject(project: Project): void {
    this.projectsService.deleteProject(project)
      .subscribe(projectResult => {
        this.projects.forEach((item, itemIndex) => {
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

    const type = this.projectTypes.find(x => x.projectTypeId === project.projectTypeId);

    if (type) {
      this.inputType = type.projectTypeId;
    }

    this.open(content);
  }

  showDeleteModal(confirm, project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.inputName = project.name;
    this.inputDescription = project.description;
    this.inputType = project.projectTypeId;

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
        newProject.projectTypeId = this.inputType;
        this.updateProject(newProject);
      } else {
        const newProject = new Project();
        newProject.name = this.inputName;
        newProject.description = this.inputDescription;
        newProject.projectTypeId = this.inputType;
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

  getFaIcon(project: Project): string {

    if (!this.projectTypes) { return; }

    const type = this.projectTypes.filter(item => item.projectTypeId === project.projectTypeId)[0];

    if (!type) { return; }

    switch (type.typeCode) {
      case 'node':
        return 'fab fa-node-js';
      case 'angular':
        return 'fab fa-angular';
      case 'winforms':
        return 'fab fa-windows';
      case 'aspcore':
        return 'fas fa-code';
    }
  }

  exportProjects(): void {
    this.exportInProgress = true;

    this.projectsService.getProjects().subscribe(
      result => {
        for (const project of result) {
          const type = this.projectTypes.filter(item => item.projectTypeId === project.projectTypeId)[0];
          project['projectType'] = type;
        }

        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        FileSave.saveAs(blob, 'projects.json');
        this.exportInProgress = false;
      }, error => { this.exportInProgress = error != null; }
    );
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
