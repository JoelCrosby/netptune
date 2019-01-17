import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { MatDialog } from '@angular/material/dialog';
import { saveAs } from 'file-saver';
import { dropIn } from '../../../animations';
import { Project } from '../../../models/project';
import { ProjectTaskCounts } from '../../../models/view-models/project-task-counts';
import { AlertService } from '../../../services/alert/alert.service';
import { ProjectTaskService } from '../../../services/project-task/project-task.service';
import { ProjectsService } from '../../../services/projects/projects.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';
import { ConfirmDialogComponent } from '../../../dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectDialogComponent } from '../../../dialogs/project-dialog/project-dialog.component';


@Component({
  selector: 'app-projects',
  templateUrl: './projects.index.component.html',
  styleUrls: ['./projects.index.component.scss'],
  animations: [dropIn]
})
export class ProjectsComponent implements OnInit {

  public exportInProgress = false;
  public taskCounts: ProjectTaskCounts[] = [];

  selectedProject: Project;

  constructor(
    public projectsService: ProjectsService,
    public projectTaskService: ProjectTaskService,
    private alertsService: AlertService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) {

  }

  async ngOnInit(): Promise<void> {
    await this.refreshData();
  }

  async refreshData(): Promise<void> {
    await this.projectsService.refreshProjects(this.workspaceService.currentWorkspace);
    await this.getProjectTaskCounts();
  }

  trackById(index: number, project: Project) {
    return project.id;
  }

  async getProjectTaskCounts(): Promise<void> {
    for (let i = 0; i < this.projectsService.projects.length; i++) {
      const project = this.projectsService.projects[i];
      this.taskCounts.push(
        await this.projectTaskService.getProjectTaskCount(project.id).toPromise()
      );
    }
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
          if (item.id === projectResult.id) {
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
    if (project === null) { return; }

    this.selectedProject = project;
    this.open(this.selectedProject);
  }

  showDeleteModal(project: Project): void {
    if (project === null) { return; }

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
        updatedProject.id = this.selectedProject.id;
        updatedProject.name = result.name;
        updatedProject.description = result.description;
        updatedProject.workspaceId = this.workspaceService.currentWorkspace.id;
        this.updateProject(updatedProject);
      } else {
        const newProject = new Project();
        newProject.name = result.name;
        newProject.description = result.description;
        newProject.workspaceId = this.workspaceService.currentWorkspace.id;
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

        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, 'projects.json');
        this.exportInProgress = false;
      }, error => { this.exportInProgress = error != null; }
    );
  }

}
