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
  styleUrls: ['./project-tasks.component.scss'],
  animations: [dropIn]
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

  trackById(index: number, task: ProjectTask) {
    return task.projectTaskId;
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

  updateProject(task: ProjectTask): void {
    this.projectTaskService.updateTask(task)
      .subscribe(result => {
        task = result;
        this.refreshData();
        this.alertsService.changeSuccessMessage('Task updated!');
      });
  }

  deleteProjectTask(task: ProjectTask): void {
    this.projectTaskService.deleteTask(task)
      .subscribe(taskResult => {

        this.projectTaskService.tasks.forEach((item, itemIndex) => {
          if (item.projectTaskId === taskResult.projectTaskId) {
            this.projectTaskService.tasks.splice(itemIndex, 1);
            this.alertsService.changeSuccessMessage('Task deleted!');

            this.snackBar.open('Task Deleted.', 'Undo', {
              duration: 3000,
            });
          }
        });

      }, error => {
        this.snackBar.open('An error occured while trying to delete Task' + error, null, {
          duration: 2000,
        });
      });
  }

  showAddModal(): void {
    this.open();
  }

  showUpdateModal(task: ProjectTask): void {
    if (task == null) { return; }

    this.selectedTask = task;
    this.open(this.selectedTask);
  }

  showDeleteModal(task: ProjectTask): void {
    if (task == null) { return; }

    this.selectedTask = task;
    this.openConfirmationDialog(this.selectedTask);
  }

  open(task?: ProjectTask): void {

    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px',
      data: task
    });

    dialogRef.afterClosed().subscribe((result: Project) => {

      if (!result) { return; }

      if (this.selectedTask) {

        const updatedProjectTask = new ProjectTask();
        updatedProjectTask.projectTaskId = this.selectedTask.projectTaskId;
        updatedProjectTask.name = result.name;
        updatedProjectTask.description = result.description;
        this.updateProject(updatedProjectTask);
      } else {
        const newProject = new ProjectTask();
        newProject.name = result.name;
        newProject.description = result.description;
        newProject.projectId = result.projectId;
        newProject.project = this.projectsService.projects.find(x => x.projectId === result.projectId);
        this.addProjectTask(newProject);
      }

      this.clearModalValues();
    });
  }

  openConfirmationDialog(task: ProjectTask) {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Remove Task',
        content: `Are you sure you wish to remove ${task.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {

      if (result) {
        this.deleteProjectTask(task);
      }

      this.clearModalValues();

    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedTask = null;
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
