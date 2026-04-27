import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import {
  selectCanDeleteTask,
} from '@app/core/store/permissions/permissions.selectors';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { LucideTrash2 } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-actions',
  template: `
    @if (canDeleteTask()) {
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Actions</h4>
        <div class="flex gap-2">
          <button
            app-stroked-button
            aria-label="Delete Task"
            appTooltip="Delete Task"
            (click)="deleteClicked()">
            <svg lucideTrash2 class="h-4 w-4"></svg>
          </button>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideTrash2, StrokedButtonComponent, TooltipDirective],
})
export class TaskDetailActionsComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);

  canDeleteTask = selectCanDeleteTask(this.store);

  deleteClicked() {
    this.taskDetailService.deleteTask();
  }
}
