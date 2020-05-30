import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { toggleChip } from '@core/animations/animations';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { TextHelpers } from '@core/util/text-helpers';

import * as TaskActions from '@project-tasks/store/tasks.actions';

@Component({
  selector: '[app-task-list-item]',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [toggleChip],
})
export class TaskListItemComponent {
  @Input() task: TaskViewModel;

  checked = false;

  constructor(private store: Store<AppState>, public dialog: MatDialog) {}

  titleClicked() {
    this.store.dispatch(TaskActions.selectTask({ task: this.task }));
  }

  editClicked() {
    this.store.dispatch(TaskActions.editProjectTask({ task: this.task }));
  }

  deleteClicked() {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '600px',
        data: {
          title: 'Are you sure you want to delete task?',
          content: `Delete task - ${TextHelpers.truncate(this.task.name)}`,
          confirm: 'Delete',
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result) {
          this.store.dispatch(
            TaskActions.deleteProjectTask({ task: this.task })
          );
        }
      });
  }

  markCompleteClicked() {
    this.store.dispatch(
      TaskActions.editProjectTask({
        task: {
          ...this.task,
          status: TaskStatus.Complete,
        },
      })
    );
  }

  moveToBacklogClicked() {
    this.store.dispatch(
      TaskActions.editProjectTask({
        task: {
          ...this.task,
          status: TaskStatus.InActive,
        },
      })
    );
  }
}
