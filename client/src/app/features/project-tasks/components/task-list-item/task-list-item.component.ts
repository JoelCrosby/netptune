import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';
import { TaskStatus } from '@core/enums/project-task-status';
import * as actions from '@core/store/tasks/tasks.actions';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListItemComponent {
  @Input() task!: TaskViewModel;

  constructor(
    private store: Store,
    private dialog: MatDialog
  ) {}

  titleClicked() {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: this.task,
      autoFocus: false,
      panelClass: 'app-modal-class',
    });
  }

  moveTask(task: TaskViewModel, sortOrder: number) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceKey}`,
        task: {
          ...task,
          sortOrder,
        },
      })
    );
  }

  deleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.deleteProjectTask({
        identifier: `[workspace] ${this.task.workspaceKey}`,
        task,
      })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceKey}`,
        task: {
          ...task,
          status: TaskStatus.complete,
        },
      })
    );
  }

  moveToBacklogClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceKey}`,
        task: {
          ...task,
          status: TaskStatus.inActive,
        },
      })
    );
  }
}
