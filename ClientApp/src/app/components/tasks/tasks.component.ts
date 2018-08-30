import { Component, OnInit } from '@angular/core';
import { MatSnackBar, MatDialog } from '@angular/material';
import { ProjectTaskService } from '../../services/project-task/project-task.service';
import { AlertService } from '../../services/alert/alert.service';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { saveAs } from 'file-saver/FileSaver';
import { ProjectTask } from '../../models/project-task';
import { Project } from '../../models/project';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { TaskDialogComponent } from '../dialogs/task-dialog/task-dialog.component';
import { dropIn } from '../../animations';

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.component.html',
  styleUrls: ['./tasks.component.scss'],
  animations: [dropIn]
})
export class TasksComponent implements OnInit {

  public exportInProgress = false;

  selectedTask: ProjectTask;

  constructor(
    public projectTaskService: ProjectTaskService,
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
    return task.taskId;
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

  deleteProject(task: ProjectTask): void {
    this.projectTaskService.deleteTask(task)
      .subscribe(taskResult => {
        this.projectTaskService.tasks.forEach((item, itemIndex) => {
          if (item.taskId === taskResult.taskId) {
            this.projectTaskService.tasks.splice(itemIndex, 1);
            this.alertsService.changeSuccessMessage('Task deleted!');
          }
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
        updatedProjectTask.taskId = this.selectedTask.taskId;
        updatedProjectTask.name = result.name;
        updatedProjectTask.description = result.description;
        this.updateProject(updatedProjectTask);
      } else {
        const newProject = new ProjectTask();
        newProject.name = result.name;
        newProject.description = result.description;
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
        this.projectTaskService.deleteTask(task)
          .subscribe((data: ProjectTask) => {
            task = data;
            this.refreshData();
            this.snackBar.open('Task Deleted.', 'Undo', {
              duration: 3000,
            });

          }, error => {
            this.snackBar.open('An error occured while trying to delete Task' + error, null, {
              duration: 2000,
            });
          });
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
