import { Component, computed, inject } from '@angular/core';
import { selectCanDeleteTask } from '@app/core/store/permissions/permissions.selectors';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { LucideSparkles, LucideTrash2 } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-actions',
  template: `
    @if (hasActions()) {
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">
          <span i18n="Section heading for actions available on a task">
            Actions
          </span>
        </h4>
        <div class="flex gap-2">
          @if (canAskAssistant()) {
            <button
              app-stroked-button
              i18n-aria-label="
                Accessible label for the button that asks the assistant about
                this task
              "
              aria-label="Ask the assistant"
              i18n-appTooltip="
                Tooltip on the button that asks the assistant about this task
              "
              appTooltip="Ask the assistant about this task"
              (click)="askAssistant()">
              <svg lucideSparkles class="h-4 w-4"></svg>
            </button>
          }

          @if (canDeleteTask()) {
            <button
              app-stroked-button
              i18n-aria-label="
                Accessible label for the button that deletes the task
              "
              aria-label="Delete Task"
              i18n-appTooltip="Tooltip on the button that deletes the task"
              appTooltip="Delete Task"
              (click)="deleteClicked()">
              <svg lucideTrash2 class="h-4 w-4"></svg>
            </button>
          }
        </div>
      </div>
    }
  `,
  imports: [
    LucideSparkles,
    LucideTrash2,
    StrokedButtonComponent,
    TooltipDirective,
  ],
})
export class TaskDetailActionsComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);

  private readonly assistant = inject(AiAssistantService);

  canDeleteTask = selectCanDeleteTask(this.store);

  protected readonly canAskAssistant = computed(() => {
    return (
      this.assistant.isAvailable() && this.taskDetailService.task() !== null
    );
  });

  protected readonly hasActions = computed(() => {
    return this.canDeleteTask() || this.canAskAssistant();
  });

  protected askAssistant() {
    const task = this.taskDetailService.task();

    if (!task) {
      return;
    }

    this.assistant.askAboutTask(task);
  }

  deleteClicked() {
    this.taskDetailService.deleteTask();
  }
}
