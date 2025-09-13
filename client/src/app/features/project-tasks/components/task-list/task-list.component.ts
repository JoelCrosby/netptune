import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { Store } from '@ngrx/store';
import { TaskListGroupComponent } from '../task-list-group/task-list-group.component';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-task-list',
    templateUrl: './task-list.component.html',
    styleUrls: ['./task-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TaskListGroupComponent, AsyncPipe]
})
export class TaskListComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  tasks$ = this.store.select(TaskSelectors.selectTasks);

  showAddModal() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: '600px',
    });
  }
}
