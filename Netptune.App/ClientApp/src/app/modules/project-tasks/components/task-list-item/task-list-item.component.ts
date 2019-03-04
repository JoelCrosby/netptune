
import { Component, Input, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn, toggleChip } from '@app/core/animations/animations';
import { Maybe } from '@app/core/types/nothing';
import { ConfirmDialogComponent } from '@app/dialogs/confirm-dialog/confirm-dialog.component';
import { TaskDialogComponent } from '@app/dialogs/task-dialog/task-dialog.component';
import { ProjectTaskStatus } from '@app/enums/project-task-status';
import { Project } from '@app/models/project';
import { ProjectTask } from '@app/models/project-task';
import { ProjectTaskService } from '@app/services/project-task/project-task.service';
import { UserService } from '@app/services/user/user.service';
import { WorkspaceService } from '@app/services/workspace/workspace.service';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [dropIn, toggleChip]
})
export class TaskListItemComponent {

  @Input() task: ProjectTask;

  selectedTask: Maybe<ProjectTask>;

  constructor(
    public dialog: MatDialog,
    public snackBar: MatSnackBar,
    public userService: UserService,
    public projectTaskService: ProjectTaskService,
    private workspaceService: WorkspaceService,
  ) { }

  clearModalValues(): void {
    this.selectedTask = null;
  }

  refreshData(): void {
    this.projectTaskService.refreshTasks(this.workspaceService.currentWorkspace);
  }

  showUpdateModal(task: ProjectTask): void {
    if (task == null) {
      return;
    }

    this.selectedTask = task;
    this.open(this.selectedTask);
  }

  showDeleteModal(task: ProjectTask): void {
    if (task == null) {
      return;
    }

    this.selectedTask = task;
    this.openConfirmationDialog(this.selectedTask);
  }

  getStatusClass(task: ProjectTask): string {
    switch (task.status) {
      case ProjectTaskStatus.Complete:
        return 'fas fa-check completed';
      case ProjectTaskStatus.InProgress:
        return 'fas fa-minus in-progress';
      case ProjectTaskStatus.OnHold:
        return 'fas fa-minus-circle blocked';
      default:
        return 'fas fa-stream none';
    }
  }

  async statusClicked(task: ProjectTask, status: ProjectTaskStatus): Promise<void> {
    await this.projectTaskService.changeTaskStatus(task, status);
  }

  open(task?: ProjectTask): void {
    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px',
      data: task
    });

    dialogRef.afterClosed().subscribe(async (result: Project) => {
      if (!result) {
        return;
      }

      if (!this.selectedTask) { throw new Error('selectedTask was undefenied.'); }

      const updatedProjectTask = new ProjectTask();
      updatedProjectTask.id = this.selectedTask.id;
      updatedProjectTask.name = result.name;
      updatedProjectTask.description = result.description;
      await this.projectTaskService.updateProjectTask(updatedProjectTask);

      this.clearModalValues();
    });
  }

  openConfirmationDialog(task: ProjectTask): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Delete Task',
        content: `Are you sure you wish to delete ${task.name}?`,
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

  async deleteProjectTask(task: ProjectTask): Promise<void> {
    await this.projectTaskService.deleteProjectTask(task);
  }
}
