import { Component, OnInit } from '@angular/core';
import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects/projects.service';
import { AlertService } from '../../services/alert/alert.service';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { ProjectType } from '../../models/project-type';
import { saveAs } from 'file-saver';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { MatDialog } from '@angular/material/dialog';
import { ProjectDialogComponent } from '../dialogs/project-dialog/project-dialog.component';
import { dropIn } from '../../animations';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { MatSnackBar } from '@angular/material';


@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.scss'],
  animations: [dropIn]
})
export class ProjectsComponent implements OnInit {

  public exportInProgress = false;

  selectedProject: Project;

  constructor(
    public projectsService: ProjectsService,
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) {

  }

  ngOnInit(): void {
    this.refreshData();
  }

  refreshData(): void {
    this.projectsService.refreshProjects(this.workspaceService.currentWorkspace);
    this.projectTypeService.refreshProjectTypes();
  }

  trackById(index: number, project: Project) {
    return project.projectId;
  }

  getProjectTypeName(project: Project): string {
    if (!project.projectTypeId || !this.projectTypeService.projectTypes) { return; }
    return this.projectTypeService.projectTypes.filter(item => item.id === project.projectTypeId)[0].name;
  }

  addProject(project: Project): void {
    this.projectsService.addProject(project)
      .subscribe((projectResult) => {
        if (projectResult) {
          this.refreshData();
          this.snackBar.open(`Project ${projectResult.name} Added!.`, null, {
            duration: 3000,
          });
          this.alertsService.changeSuccessMessage(`Project ${projectResult.name} Added!.`);
        }
      }, error => {
        this.snackBar.open('An error occured while trying to create the project. ' + error, null, {
          duration: 2000,
        });
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Project. ' + error);
      });
  }

  updateProject(project: Project): void {
    this.projectsService.updateProject(project)
      .subscribe(result => {
        project = result;
        this.refreshData();
        this.alertsService.changeSuccessMessage('Project updated!');
      });
  }

  deleteProject(project: Project): void {
    this.projectsService.deleteProject(project)
      .subscribe(projectResult => {
        this.projectsService.projects.forEach((item, itemIndex) => {
          if (item.projectId === projectResult.projectId) {
            this.projectsService.projects.splice(itemIndex, 1);
            this.alertsService.changeSuccessMessage('Project deleted!');
          }
        });
      });
  }

  showAddModal(): void {
    this.open();
  }

  showUpdateModal(project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.open(this.selectedProject);
  }

  showDeleteModal(project: Project): void {
    if (project == null) { return; }

    this.selectedProject = project;
    this.openConfirmationDialog(this.selectedProject);
  }

  open(project?: Project): void {

    const dialogRef = this.dialog.open(ProjectDialogComponent, {
      width: '600px',
      data: project
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
        newProject.workspaceId = this.workspaceService.currentWorkspace.workspaceId;
        this.addProject(newProject);
      }

      this.clearModalValues();
    });
  }

  openConfirmationDialog(project: Project) {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Remove Project',
        content: `Are you sure you wish to remove ${project.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: Project) => {

      if (result) {
        this.projectsService.deleteProject(project)
          .subscribe((data: Project) => {
            project = data;
            this.refreshData();
            this.snackBar.open('Project Deleted.', 'Undo', {
              duration: 3000,
            });

          }, error => {
            this.snackBar.open('An error occured while trying to delete project' + error, null, {
              duration: 2000,
            });
          });
      }

      this.clearModalValues();

    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedProject = null;
  }

  exportProjects(): void {
    this.exportInProgress = true;

    this.projectsService.getProjects(this.workspaceService.currentWorkspace).subscribe(
      result => {
        for (const project of result) {
          const type = this.projectTypeService.projectTypes.filter(item => item.id === project.projectTypeId)[0];
          project['projectType'] = type;
        }

        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, 'projects.json');
        this.exportInProgress = false;
      }, error => { this.exportInProgress = error != null; }
    );
  }

}
