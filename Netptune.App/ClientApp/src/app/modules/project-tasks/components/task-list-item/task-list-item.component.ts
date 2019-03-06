
import { Component, Input } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { toggleChip } from '@app/core/animations/animations';
import { Maybe } from '@app/core/types/nothing';
import { ConfirmDialogComponent } from '@app/dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectTaskStatus } from '@app/enums/project-task-status';
import { ProjectTask } from '@app/models/project-task';
import { ProjectTaskService } from '@app/services/project-task/project-task.service';
import { UserService } from '@app/services/user/user.service';
import { WorkspaceService } from '@app/services/workspace/workspace.service';
import { TaskDetailDialogComponent } from '../../../../dialogs/task-detail-dialog/task-detail-dialog.component';
import { ProjectTaskDto } from '../../../../models/view-models/project-task-dto';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [toggleChip]
})
export class TaskListItemComponent {

  @Input() task: ProjectTaskDto;

  constructor(
    public dialog: MatDialog,
    public snackBar: MatSnackBar,
    public userService: UserService,
    public projectTaskService: ProjectTaskService,
    private workspaceService: WorkspaceService,
  ) { }

  refreshData(): void {
    this.projectTaskService.refreshTasks(this.workspaceService.currentWorkspace);
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

  async openDetail(): Promise<void> {

    const task = await this.projectTaskService.getTask(this.task.id).toPromise();

    console.log(task);

    const dialogRef = this.dialog.open(TaskDetailDialogComponent, {
      width: '800px',
      data: task
    });

    dialogRef.afterClosed().subscribe(async (result: ProjectTask) => {
      if (!result) {
        return;
      }
      await this.projectTaskService.updateProjectTask(result);
    });
  }

  openConfirmationDialog(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Delete Task',
        content: `Are you sure you wish to delete ${this.task.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {
      if (result) {
        this.deleteProjectTask();
      }
    });
  }

  async deleteProjectTask(): Promise<void> {
    // await this.projectTaskService.deleteProjectTask(this.task);
  }
}
