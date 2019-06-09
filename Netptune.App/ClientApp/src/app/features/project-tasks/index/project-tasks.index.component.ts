import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn, fadeIn } from '@app/core/animations/animations';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { AppState } from '@app/core/core.state';
import { ActionLoadProjectTasks } from '../store/project-tasks.actions';
import {
  selectTasksBacklog,
  selectTasksCompleted,
  selectTasksOwner,
  selectTasksLoaded,
  selectSelectedTask,
} from '../store/project-tasks.selectors';
import { TaskDialogComponent } from '@app/shared/dialogs/task-dialog/task-dialog.component';
import { ActionLoadProjects } from '@app/features/projects/store/projects.actions';

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

  loaded$ = this.store.select(selectTasksLoaded);

  selectedTask$ = this.store.select(selectSelectedTask);

  taskGroups = [
    {
      groupName: 'my-tasks',
      tasks: this.myTasks$,
      header: 'My Tasks',
      emptyMessage: 'You have no tasks. Click the button in the bottom right to create a task.',
    },
    {
      groupName: 'completed-tasks',
      tasks: this.completedTasks$,
      header: 'Completed Tasks',
      emptyMessage:
        'You currently have no completed tasks. Mark a task as completed and it will show up here.',
    },
    {
      groupName: 'backlog-tasks',
      tasks: this.backlogTasks$,
      header: 'Backlog',
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
