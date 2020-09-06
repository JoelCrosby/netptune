import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TaskStatus } from '@core/enums/project-task-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

interface TaskListGroup {
  groupName: string;
  tasks: Observable<TaskViewModel[]>;
  header: string;
  status: TaskStatus;
  emptyMessage: string;
}

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent implements OnInit, AfterViewInit {
  loaded$: Observable<boolean>;
  selectedTask$: Observable<TaskViewModel>;

  taskGroups: TaskListGroup[];

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store
  ) {}

  ngOnInit() {
    this.loaded$ = this.store.select(TaskSelectors.selectTasksLoaded);
    this.selectedTask$ = this.store.select(TaskSelectors.selectSelectedTask);

    this.taskGroups = [
      {
        groupName: 'my-tasks',
        tasks: this.store.select(TaskSelectors.selectTasksOwner),
        header: 'My Tasks',
        status: TaskStatus.New,
        emptyMessage: 'You have no tasks assigned to you',
      },
      {
        groupName: 'completed-tasks',
        tasks: this.store.select(TaskSelectors.selectTasksCompleted),
        header: 'Completed Tasks',
        status: TaskStatus.Complete,
        emptyMessage: 'You currently have no completed tasks.',
      },
      {
        groupName: 'backlog-tasks',
        tasks: this.store.select(TaskSelectors.selectTasksBacklog),
        header: 'Backlog',
        status: TaskStatus.InActive,
        emptyMessage: 'Your backlog is currently empty hurray!',
      },
    ];
  }

  ngAfterViewInit() {
    this.store.dispatch(TaskActions.loadProjectTasks());
  }

  showAddModal() {
    this.dialog.open(TaskDialogComponent, {
      width: '600px',
    });
  }
}
