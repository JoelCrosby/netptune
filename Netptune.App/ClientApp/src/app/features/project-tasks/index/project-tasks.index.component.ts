import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn, fadeIn } from '@app/core/animations/animations';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';
import { ProjectTask } from '@app/core/models/project-task';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn, dropIn],
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

  Complete = ProjectTaskStatus.Complete;
  InProgress = ProjectTaskStatus.InProgress;
  Blocked = ProjectTaskStatus.OnHold;

  constructor(public snackBar: MatSnackBar, public dialog: MatDialog) {}

  ngOnInit(): void {
    this.refreshData();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  async refreshData(): Promise<void> {}

  drop(event: CdkDragDrop<ProjectTaskDto[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }
  }
}
