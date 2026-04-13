import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { selectRequiredDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { LucideFlag, LucideTrash2 } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-actions',
  template: `
    @if (showActions()) {
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Actions</h4>
        <div class="flex gap-2">
          @if (canDeleteTask()) {
            <button
              app-stroked-button
              aria-label="Delete Task"
              appTooltip="Delete Task"
              (click)="deleteClicked()">
              <svg lucideTrash2 class="h-4 w-4"></svg>
            </button>
          }

          @if (canUpdateTask()) {
            <button
              app-stroked-button
              aria-label="Flag Task"
              appTooltip="Flag Task"
              color="warn"
              (click)="onFlagClicked()">
              <svg
                lucideFlag
                class="h-4 w-4"
                [class.fill-current]="task().isFlagged"></svg>
            </button>
          }
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideFlag, LucideTrash2, StrokedButtonComponent, TooltipDirective],
})
export class TaskDetailActionsComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);

  canUpdateTask = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.update)
  );

  canDeleteTask = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.delete)
  );

  showActions = computed(() => this.canUpdateTask() || this.canDeleteTask());

  onFlagClicked() {
    const task = this.task();

    if (!task) return;

    const updated: UpdateProjectTaskRequest = {
      ...task,
      isFlagged: !task.isFlagged,
    };

    this.taskDetailService.updateTask(updated);
  }

  deleteClicked() {
    this.taskDetailService.deleteTask();
  }
}
