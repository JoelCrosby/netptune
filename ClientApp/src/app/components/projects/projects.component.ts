import { Component, OnInit } from '@angular/core';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';
import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects/projects.service';
import { AlertService } from '../../services/alert/alert.service';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { ProjectType } from '../../models/project-type';
import { saveAs } from 'file-saver/FileSaver';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { MatDialog } from '@angular/material/dialog';
import { ProjectDialogComponent } from './dialogs/project-dialog/project-dialog.component';


@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.scss']
})
export class ProjectsComponent implements OnInit {

  public projects: Project[];
  public projectTypes: ProjectType[];

  public showAddForm = false;
  public exportInProgress = false;

  selectedProject: Project;

  constructor(
    private projectsService: ProjectsService,
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private workspaceService: WorkspaceService,
    private modalService: NgbModal,
    public dialog: MatDialog) {

  }

  ngOnInit(): void {
    this.getProjects();
    this.getProjectTypes();
  }

  trackById(index: number, project: Project) {
    return project.projectId;
  }

  getProjects(): void {
    this.projectsService.getProjects(this.workspaceService.currentWorkspace)
      .subscribe(projects => this.projects = projects);
  }

  getProjectTypes(): void {
    this.projectTypeService.getProjectTypes()
      .subscribe(projectTypes => this.projectTypes = projectTypes);
  }

  getProjectTypeName(project: Project): string {
    if (!project.projectTypeId || !this.projectTypes) { return; }
    return this.projectTypes.filter(item => item.id === project.projectTypeId)[0].name;
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

  showAddModal(): void {
    this.open();
  }

  showUpdateModal(project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.open();
  }

  showDeleteModal(project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.openConfirmationDialog(this.selectedProject);
  }

  open(): void {

    const dialogRef = this.dialog.open(ProjectDialogComponent, {
      width: '500px',
      data: this.selectedProject
    });

    dialogRef.afterClosed().subscribe((result: Project) => {

      if (!result) { return; }

      if (this.selectedProject) {

        const updatedProject = new Project();
        updatedProject.projectId = this.selectedProject.projectId;
        updatedProject.name = result.name;
        updatedProject.description = result.description;
        updatedProject.projectTypeId = result.projectTypeId;
        updatedProject.workspaceId = this.workspaceService.currentWorkspace.workspaceId;
        this.updateProject(updatedProject);
      } else {
        const newProject = new Project();
        newProject.name = result.name;
        newProject.description = result.description;
        newProject.projectTypeId = result.projectTypeId;
        console.log('this.inputType: ' + result.projectTypeId);
        newProject.workspaceId = this.workspaceService.currentWorkspace.workspaceId;
        this.addProject(newProject);
      }
    });
  }

  openConfirmationDialog(project: Project) {

      if (!project) {
        return;
      } else {
        this.deleteProject(project);
      }

  }

  getFaIcon(project: Project): string {

    if (!this.projectTypes) { return; }

    const type = this.projectTypes.filter(item => item.id === project.projectTypeId)[0];

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

    this.projectsService.getProjects(this.workspaceService.currentWorkspace).subscribe(
      result => {
        for (const project of result) {
          const type = this.projectTypes.filter(item => item.id === project.projectTypeId)[0];
          project['projectType'] = type;
        }

        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, 'projects.json');
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
