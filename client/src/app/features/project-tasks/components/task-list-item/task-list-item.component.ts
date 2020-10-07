import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
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
  @Input() task: TaskViewModel;

  constructor(private store: Store, public dialog: MatDialog) {}

  titleClicked() {
    this.dialog.open(TaskDetailDialogComponent, {
      width: '800px',
      data: this.task,
    });
  }

  moveTask(task: TaskViewModel, sortOrder: number) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceSlug}`,
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
        identifier: `[workspace] ${this.task.workspaceSlug}`,
        task,
      })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceSlug}`,
        task: {
          ...task,
          status: TaskStatus.Complete,
        },
      })
    );
  }

  moveToBacklogClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task.workspaceSlug}`,
        task: {
          ...task,
          status: TaskStatus.InActive,
        },
      })
    );
  }
}