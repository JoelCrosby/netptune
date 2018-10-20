import { Component, OnInit, Input, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { ProjectTask, ProjectTaskStatus } from '../../../models/project-task';
import { MatDialog, MatSnackBar } from '@angular/material';
import { TaskDialogComponent } from '../../dialogs/task-dialog/task-dialog.component';
import { Project } from '../../../models/project';
import { ProjectTaskService } from '../../../services/project-task/project-task.service';
import { ProjectsService } from '../../../services/projects/projects.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';
import { AlertService } from '../../../services/alert/alert.service';
import { ConfirmDialogComponent } from '../../dialogs/confirm-dialog/confirm-dialog.component';
import { dropIn, toggleChip } from '../../../animations';
import { Subscription } from 'rxjs';
import { DragulaService } from 'ng2-dragula';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  animations: [dropIn, toggleChip]
})
export class TaskListComponent implements OnInit, OnDestroy {

  @Input() tasks: ProjectTask[];
  @Input() dragGroupName: string;

  public selectedTask: ProjectTask;

  subs = new Subscription();

  Complete = ProjectTaskStatus.Complete;
  InProgress = ProjectTaskStatus.InProgress;
  Blocked = ProjectTaskStatus.OnHold;

  constructor(
    public dialog: MatDialog,
    public snackBar: MatSnackBar,
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    private alertsService: AlertService,
    private dragulaService: DragulaService,
    private change: ChangeDetectorRef
  ) {

  }

  ngOnInit() {
    this.subs.add(this.dragulaService.drop(this.dragGroupName)
      .subscribe(() => {
        this.UpdateSortOrder();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
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

  async updateProjectTask(task: ProjectTask): Promise<ProjectTask> {

    try {
      const result = await this.projectTaskService.updateTask(task).toPromise();

      if (result) {
        task = result;
        this.change.detectChanges();
        this.alertsService.changeSuccessMessage('Task updated!');
        return task;
      } else {
        return null;
      }
    } catch (error) {
      this.snackBar.open('An error occured while trying to update the task. ' + error, null, {
        duration: 2000,
      });
      return null;
    }

  }

  UpdateSortOrder(): void {
    this.projectTaskService.updateSortOrder(this.tasks)
      .subscribe((responce: ProjectTask[]) => {

      });
  }

  async statusClicked(task: ProjectTask, status: ProjectTaskStatus): Promise<void> {

    const oldStatus = task.status;

    task.status = status;
    const result = await this.updateProjectTask(task);

    if (!result) {
      task.status = oldStatus;
    }
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
        this.updateProjectTask(updatedProjectTask);
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
