import { Component, OnInit, Input } from '@angular/core';
import { ProjectTask } from '../../../models/project-task';
import { MatDialog, MatSnackBar } from '@angular/material';
import { TaskDialogComponent } from '../../dialogs/task-dialog/task-dialog.component';
import { Project } from '../../../models/project';
import { ProjectTaskService } from '../../../services/project-task/project-task.service';
import { ProjectsService } from '../../../services/projects/projects.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';
import { AlertService } from '../../../services/alert/alert.service';
import { ConfirmDialogComponent } from '../../dialogs/confirm-dialog/confirm-dialog.component';
import { dropIn } from '../../../animations';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  animations: [dropIn]
})
export class TaskListComponent implements OnInit {

  @Input() tasks: ProjectTask[];

  public selectedTask: ProjectTask;

  constructor(
    public dialog: MatDialog,
    public snackBar: MatSnackBar,
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    private alertsService: AlertService,
  ) { }

  ngOnInit() {
  }

  trackById(index: number, task: ProjectTask) {
    return task.projectTaskId;
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedTask = null;
  }

  refreshData(): void {
    this.projectTaskService.refreshTasks(this.workspaceService.currentWorkspace);
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

  updateProject(task: ProjectTask): void {
    this.projectTaskService.updateTask(task)
      .subscribe(result => {
        task = result;
        this.refreshData();
        this.alertsService.changeSuccessMessage('Task updated!');
      });
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

        this.projectTaskService.myTasks.forEach((item, itemIndex) => {
          if (item.projectTaskId === taskResult.projectTaskId) {
            this.projectTaskService.myTasks.splice(itemIndex, 1);
          }
        });

      }, error => {
        this.snackBar.open('An error occured while trying to delete Task' + error, null, {
          duration: 2000,
        });
      });
  }

}
