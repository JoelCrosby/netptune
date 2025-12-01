import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';
import { TaskStatus } from '@core/enums/project-task-status';
import * as actions from '@core/store/tasks/tasks.actions';
import { MatIconButton } from '@angular/material/button';
import {
  MatMenuTrigger,
  MatMenu,
  MatMenuContent,
  MatMenuItem,
} from '@angular/material/menu';
import { MatIcon } from '@angular/material/icon';
import { MatCheckbox } from '@angular/material/checkbox';

import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatIconButton,
    MatMenuTrigger,
    MatIcon,
    MatCheckbox,
    AvatarComponent,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
  ],
})
export class TaskListItemComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly task = input.required<TaskViewModel>();

  titleClicked() {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: this.task(),
      autoFocus: false,
      panelClass: 'app-modal-class',
    });
  }

  moveTask(task: TaskViewModel, sortOrder: number) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task().workspaceKey}`,
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
        identifier: `[workspace] ${this.task().workspaceKey}`,
        task,
      })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.task().workspaceKey}`,
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
        identifier: `[workspace] ${this.task().workspaceKey}`,
        task: {
          ...task,
          status: TaskStatus.inActive,
        },
      })
    );
  }
}
