import { Component, Input } from '@angular/core';
import { toggleChip } from '@core/animations/animations';
import { ProjectTaskDto } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';
import {
  ActionEditProjectTask,
  ActionDeleteProjectTask,
  ActionSelectTask,
} from '../../store/project-tasks.actions';
import { ProjectTaskStatus } from '@core/enums/project-task-status';
import { MatDialog } from '@angular/material';
import { ConfirmDialogComponent } from '@app/shared/dialogs/confirm-dialog/confirm-dialog.component';
import { TextHelpers } from '@core/util/text-helpers';

@Component({
  selector: '[app-task-list-item]',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [toggleChip],
})
export class TaskListItemComponent {
  @Input() task: ProjectTaskDto;

  constructor(private store: Store<AppState>, public dialog: MatDialog) {}

  titleClicked() {
    this.store.dispatch(new ActionSelectTask(this.task));
  }

  editClicked() {
    this.store.dispatch(new ActionEditProjectTask(this.task));
  }

  deleteClicked() {
    this.dialog
      .open<ConfirmDialogComponent>(ConfirmDialogComponent, {
        data: {
          title: 'Are you sure you want to delete task?',
          content: `Delete task - ${TextHelpers.truncate(this.task.name)}`,
          confirm: 'Delete',
        },
      })
      .afterClosed()
      .subscribe(result => {
        if (result) {
          this.store.dispatch(new ActionDeleteProjectTask(this.task));
        }
      });
  }

  markCompleteClicked() {
    this.store.dispatch(
      new ActionEditProjectTask({
        ...this.task,
        status: ProjectTaskStatus.Complete,
      })
    );
  }

  moveToBacklogClicked() {
    this.store.dispatch(
      new ActionEditProjectTask({
        ...this.task,
        status: ProjectTaskStatus.InActive,
      })
    );
  }
}
