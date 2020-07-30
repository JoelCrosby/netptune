import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TaskStatus } from '@app/core/enums/project-task-status';
import { TaskDialogComponent } from '@app/entry/dialogs/task-dialog/task-dialog.component';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { select, Store } from '@ngrx/store';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent implements OnInit {
  myTasks$ = this.store.pipe(select(TaskSelectors.selectTasksOwner));
  completedTasks$ = this.store.pipe(select(TaskSelectors.selectTasksCompleted));
  backlogTasks$ = this.store.pipe(select(TaskSelectors.selectTasksBacklog));

  loaded$ = this.store.pipe(select(TaskSelectors.selectTasksLoaded));

  selectedTask$ = this.store.pipe(select(TaskSelectors.selectSelectedTask));

  taskGroups = [
    {
      groupName: 'my-tasks',
      tasks: this.myTasks$,
      header: 'My Tasks',
      status: TaskStatus.New,
      emptyMessage:
        'You have no tasks. Click the button in the bottom right to create a task.',
    },
    {
      groupName: 'completed-tasks',
      tasks: this.completedTasks$,
      header: 'Completed Tasks',
      status: TaskStatus.Complete,
      emptyMessage:
        'You currently have no completed tasks. Mark a task as completed and it will show up here.',
    },
    {
      groupName: 'backlog-tasks',
      tasks: this.backlogTasks$,
      header: 'Backlog',
      status: TaskStatus.InActive,
      emptyMessage: 'Your backlog is currently empty hurray!',
    },
  ];

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store
  ) {}

  ngOnInit() {
    this.store.dispatch(TaskActions.loadProjectTasks());
  }

  showAddModal() {
    this.dialog
      .open(TaskDialogComponent, {
        width: '600px',
      })
      .afterClosed()
      .subscribe(() => {});
  }
}
