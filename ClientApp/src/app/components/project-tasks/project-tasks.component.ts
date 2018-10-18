import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { saveAs } from 'file-saver';
import { dropIn } from '../../animations';
import { Project } from '../../models/project';
import { ProjectTask } from '../../models/project-task';
import { AlertService } from '../../services/alert/alert.service';
import { ProjectTaskService } from '../../services/project-task/project-task.service';
import { ProjectsService } from '../../services/projects/projects.service';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { TaskDialogComponent } from '../dialogs/task-dialog/task-dialog.component';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.component.html',
  styleUrls: ['./project-tasks.component.scss']
})
export class ProjectTasksComponent implements OnInit {

  public exportInProgress = false;

  displayedColumns: string[] = ['name', 'description', 'owner', 'status'];

  selectedTask: ProjectTask;

  constructor(
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private alertsService: AlertService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) {

  }

  ngOnInit(): void {
    this.refreshData();
  }

  refreshData(): void {
    this.projectTaskService.refreshTasks(this.workspaceService.currentWorkspace);
  }

  addProjectTask(task: ProjectTask): void {
    this.projectTaskService.addTask(task)
      .subscribe((taskResult) => {
        if (taskResult) {
          this.refreshData();
          this.snackBar.open(`Project ${taskResult.name} Added!.`, null, {
            duration: 3000,
          });
          this.alertsService.changeSuccessMessage(`Project ${taskResult.name} Added!.`);
        }
      }, error => {
        this.snackBar.open('An error occured while trying to create the task. ' + error, null, {
          duration: 2000,
        });
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the task. ' + error);
      });
  }

  showAddModal(): void {
    this.open();
  }

  open(): void {

    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px',
    });

    dialogRef.afterClosed().subscribe((result: Project) => {

      if (!result) { return; }

      const newProject = new ProjectTask();
      newProject.name = result.name;
      newProject.description = result.description;
      newProject.projectId = result.projectId;
      newProject.project = this.projectsService.projects.find(x => x.projectId === result.projectId);

      this.addProjectTask(newProject);

    });
  }

  exportProjects(): void {
    this.exportInProgress = true;

    this.projectTaskService.getTasks(this.workspaceService.currentWorkspace).subscribe(
      result => {
        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, 'projects.json');
        this.exportInProgress = false;
      }, error => { this.exportInProgress = error != null; }
    );
  }

}
