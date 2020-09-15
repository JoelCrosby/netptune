import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { TaskListGroup } from '@core/store/tasks/tasks.model';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent implements OnInit {
  loading$ = this.store.select(TaskSelectors.selectTasksLoading);
  taskGroups$ = this.store.select(TaskSelectors.selectTaskGroups);

  constructor(private dialog: MatDialog, private store: Store) {}

  ngOnInit() {
    this.store.dispatch(TaskActions.loadProjectTasks());
  }

  trackByGroup(_: number, group: TaskListGroup) {
    return group.groupName;
  }

  showAddModal() {
    this.dialog.open(TaskDialogComponent, {
      width: '600px',
    });
  }
}
