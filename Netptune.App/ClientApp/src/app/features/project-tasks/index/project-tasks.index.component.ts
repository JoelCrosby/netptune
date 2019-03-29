import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn, fadeIn } from '@app/core/animations/animations';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { AppState } from '../../../core/core.state';
import { ActionLoadProjectTasks } from '../store/project-tasks.actions';
import {
  selectTasksBacklog,
  selectTasksCompleted,
  selectTasksOwner,
} from '../store/project-tasks.selectors';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn, dropIn],
})
export class ProjectTasksComponent implements OnInit {
  myTasks$ = this.store.select(selectTasksOwner);
  completedTasks$ = this.store.select(selectTasksCompleted);
  backlogTasks$ = this.store.select(selectTasksBacklog);

  completedStatus = ProjectTaskStatus.Complete;
  inProgressStatus = ProjectTaskStatus.InProgress;
  blockedStatus = ProjectTaskStatus.OnHold;
  backlogStatus = ProjectTaskStatus.InActive;

  Complete = ProjectTaskStatus.Complete;
  InProgress = ProjectTaskStatus.InProgress;
  Blocked = ProjectTaskStatus.OnHold;

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(new ActionLoadProjectTasks());
  }

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
