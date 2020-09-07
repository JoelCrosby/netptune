import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { TaskListGroup } from '@core/store/tasks/tasks.model';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent implements OnInit, AfterViewInit {
  loading$: Observable<boolean>;
  taskGroups$: Observable<TaskListGroup[]>;

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store
  ) {}

  ngOnInit() {
    this.loading$ = this.store.select(TaskSelectors.selectTasksLoading);
    this.taskGroups$ = this.store.select(TaskSelectors.selectTaskGroups);
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
