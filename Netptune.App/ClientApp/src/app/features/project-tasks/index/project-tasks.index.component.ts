import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';
import { ActionLoadProjects } from '@app/features/projects/store/projects.actions';
import { TaskDialogComponent } from '@app/shared/dialogs/task-dialog/task-dialog.component';
import { dropIn, fadeIn } from '@core/animations/animations';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import { ActionLoadProjectTasks } from '../store/project-tasks.actions';
import {
  selectSelectedTask,
  selectTasksBacklog,
  selectTasksCompleted,
  selectTasksLoaded,
  selectTasksOwner,
} from '../store/project-tasks.selectors';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn, dropIn],
})
export class ProjectTasksComponent implements OnInit {
  myTasks$ = this.store.pipe(select(selectTasksOwner));
  completedTasks$ = this.store.pipe(select(selectTasksCompleted));
  backlogTasks$ = this.store.pipe(select(selectTasksBacklog));

  loaded$ = this.store.pipe(select(selectTasksLoaded));

  selectedTask$ = this.store.pipe(select(selectSelectedTask));

  taskGroups = [
    {
      groupName: 'my-tasks',
      tasks: this.myTasks$,
      header: 'My Tasks',
      status: ProjectTaskStatus.New,
      emptyMessage:
        'You have no tasks. Click the button in the bottom right to create a task.',
    },
    {
      groupName: 'completed-tasks',
      tasks: this.completedTasks$,
      header: 'Completed Tasks',
      status: ProjectTaskStatus.Complete,
      emptyMessage:
        'You currently have no completed tasks. Mark a task as completed and it will show up here.',
    },
    {
      groupName: 'backlog-tasks',
      tasks: this.backlogTasks$,
      header: 'Backlog',
      status: ProjectTaskStatus.InActive,
      emptyMessage: 'Your backlog is currently empty hurray!',
    },
  ];

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(new ActionLoadProjects());
    this.store.dispatch(new ActionLoadProjectTasks());
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
