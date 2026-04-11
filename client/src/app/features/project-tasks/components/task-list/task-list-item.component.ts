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
import {
  LucideArchiveRestore,
  LucideCheck,
  LucideEllipsisVertical,
  LucideFlag,
  LucideTrash2,
} from '@lucide/angular';
import { MatCheckbox } from '@angular/material/checkbox';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-task-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatIconButton,
    MatMenuTrigger,
    LucideEllipsisVertical,
    LucideFlag,
    LucideCheck,
    LucideArchiveRestore,
    LucideTrash2,
    MatCheckbox,
    AvatarComponent,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
  ],
  template: `
    <div
      class="bg-card flex h-10 cursor-pointer items-center overflow-hidden transition-colors duration-200 ease-in-out"
      [class.flagged]="task().isFlagged">
      <button
        class="w-10 flex-none"
        mat-icon-button
        aria-label="more"
        [matMenuTriggerData]="{ task: task() }"
        [matMenuTriggerFor]="menu">
        <svg lucideEllipsisVertical class="text-foreground/30 h-4 w-4"></svg>
      </button>

      <mat-checkbox class="w-8 flex-none" color="primary"></mat-checkbox>

      <div class="w-[100px] flex-none">
        <div class="bg-foreground/10 inline rounded px-1.5 py-0.5 text-sm">
          {{ task().systemId }}
        </div>
      </div>

      <div
        class="flex-1 overflow-hidden text-sm text-ellipsis whitespace-nowrap"
        aria-hidden="false"
        role="button"
        tabindex=""
        (click)="titleClicked()">
        {{ task().name }}
      </div>

      @if (task().isFlagged) {
        <svg lucideFlag class="h-5 w-8 flex-none text-red-500"></svg>
      }

      @if (task().status === 1) {
        <svg lucideCheck class="h-5 w-5 text-green-500"></svg>
      }

      @for (assignee of task().assignees; track assignee.id) {
        <app-avatar
          class="w-[38px] flex-none"
          size="24"
          [name]="assignee.displayName"
          [imageUrl]="assignee.pictureUrl">
        </app-avatar>
      }
    </div>

    <mat-menu #menu="matMenu">
      <ng-template matMenuContent let-task="task">
        <button mat-menu-item (click)="markCompleteClicked(task)">
          <svg lucideCheck class="h-4 w-4"></svg>
          <span>Mark Complete</span>
        </button>

        <button mat-menu-item (click)="moveToBacklogClicked(task)">
          <svg lucideArchiveRestore class="h-4 w-4"></svg>
          <span>Move to Backlog</span>
        </button>

        <button mat-menu-item (click)="deleteClicked(task)">
          <svg lucideTrash2 class="h-4 w-4"></svg>
          <span>Delete</span>
        </button>
      </ng-template>
    </mat-menu>
  `,
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
