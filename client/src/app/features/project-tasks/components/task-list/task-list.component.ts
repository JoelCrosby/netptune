import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { AppState } from '@core/core.state';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListComponent {
  tasks$ = this.store.select(TaskSelectors.selectTasks);

  constructor(private dialog: MatDialog, private store: Store<AppState>) {}

  showAddModal() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: '600px',
    });
  }
}
