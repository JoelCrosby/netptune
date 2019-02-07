import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatExpansionPanel, MatSnackBar } from '@angular/material';
import { Subscription } from 'rxjs';
import { fadeIn } from '@app/core/animations/animations';
import { TaskDialogComponent } from '@app/dialogs/task-dialog/task-dialog.component';
import { ProjectTaskStatus } from '@app/enums/project-task-status';
import { ProjectTask } from '@app/models/project-task';
import { ProjectTaskDto } from '@app/models/view-models/project-task-dto';
import { ProjectTaskService } from '@app/services/project-task/project-task.service';
import { UtilService } from '@app/services/util/util.service';
import { WorkspaceService } from '@app/services/workspace/workspace.service';
import { TaskDetailDialogComponent } from '@app/dialogs/task-detail-dialog/task-detail-dialog.component';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn]
})
export class ProjectTasksComponent implements OnInit, OnDestroy {

  exportInProgress = false;

  selectedTask: ProjectTask;
  subs = new Subscription();

  completedStatus = ProjectTaskStatus.Complete;
  inProgressStatus = ProjectTaskStatus.InProgress;
  blockedStatus = ProjectTaskStatus.OnHold;
  backlogStatus = ProjectTaskStatus.InActive;

  myTasks: ProjectTaskDto[] = [];
  completedTasks: ProjectTaskDto[] = [];
  backlogTasks: ProjectTaskDto[] = [];

  completedTasksPeers: string[] = ['myTasks', 'backlogTasks'];
  inProgressTasksPeers: string[] = ['completedTasks', 'backlogTasks'];
  backlogTasksPeers: string[] = ['completedTasks', 'myTasks'];

  taskspanel: MatExpansionPanel;

  dataLoaded = false;

  constructor(
    public projectTaskService: ProjectTaskService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    private utilService: UtilService,
    public dialog: MatDialog
  ) {
    this.subs.add(this.projectTaskService.taskUpdated
      .subscribe(async _ => await this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskAdded
      .subscribe(async _ => await this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskDeleted
      .subscribe(async _ => await this.refreshData())
    );
  }

  ngOnInit(): void {
    this.refreshData();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  async refreshData(): Promise<void> {
    await this.projectTaskService.refreshTasks();

    this.utilService.smoothUpdate(this.myTasks, this.projectTaskService.myTasks);
    this.utilService.smoothUpdate(this.completedTasks, this.projectTaskService.completedTasks);
    this.utilService.smoothUpdate(this.backlogTasks, this.projectTaskService.backlogTasks);

    this.dataLoaded = true;
  }

  async addProjectTask(task: ProjectTask): Promise<void> {
    await this.projectTaskService.addProjectTask(task);
  }

  showAddModal(): void {

    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {
      if (!result) {
        return;
      }
      this.addProjectTask(result);
    });
  }

  showDetailModal(): void {

    const dialogRef = this.dialog.open(TaskDetailDialogComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {
      if (!result) {
        return;
      }
      this.addProjectTask(result);
    });
  }
}
