import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { TaskStatus } from '@app/core/enums/project-task-status';
import { TaskDialogComponent } from '@app/shared/dialogs/task-dialog/task-dialog.component';
import { dropIn, fadeIn } from '@core/animations/animations';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import * as actions from '../store/tasks.actions';
import * as selectors from '../store/tasks.selectors';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn, dropIn],
})
export class ProjectTasksComponent implements OnInit {
  myTasks$ = this.store.pipe(select(selectors.selectTasksOwner));
  completedTasks$ = this.store.pipe(select(selectors.selectTasksCompleted));
  backlogTasks$ = this.store.pipe(select(selectors.selectTasksBacklog));

  loaded$ = this.store.pipe(select(selectors.selectTasksLoaded));

  selectedTask$ = this.store.pipe(select(selectors.selectSelectedTask));

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
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(actions.loadProjectTasks());
  }

  showAddModal() {
    this.dialog
      .open<TaskDialogComponent>(TaskDialogComponent, {
        width: '600px',
      })
      .afterClosed()
      .subscribe(() => {});
  }
}
