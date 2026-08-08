import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SprintBacklogPriorityClassPipe } from '../pipes/sprint-backlog-priority-class.pipe';
import { SprintBacklogPriorityLabelPipe } from '../pipes/sprint-backlog-priority-label.pipe';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';

@Component({
  selector: 'app-sprint-backlog-task-row',
  host: {
    class:
      'border-border flex items-center justify-between gap-4 border-b p-4 last:border-b-0',
  },
  imports: [
    RouterLink,
    FlatButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    TaskScopeIdComponent,
    BadgeComponent,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogStatusLabelPipe,
    SprintBacklogPriorityClassPipe,
    SprintBacklogPriorityLabelPipe,
  ],
  template: `
    <div class="min-w-0 flex-1">
      <div class="flex flex-wrap items-center gap-2">
        <app-task-scope-id [id]="task().systemId" />
        <a
          class="truncate font-medium"
          [routerLink]="['../../tasks', task().systemId]">
          {{ task().name }}
        </a>
      </div>
      <div class="mt-1.5 flex flex-wrap items-center gap-2">
        <app-badge
          shape="rounded"
          [class]="
            'px-1.5 ' + (task().statusCategory | sprintBacklogStatusBadgeClass)
          ">
          {{ task().statusName | sprintBacklogStatusLabel }}
        </app-badge>
        @if (task().priority !== null && task().priority !== undefined) {
          <span
            class="text-xs font-medium"
            [class]="task().priority | sprintBacklogPriorityClass">
            {{ task().priority | sprintBacklogPriorityLabel }}
          </span>
        }
        <span class="text-muted text-xs">{{ task().projectName }}</span>
      </div>
    </div>

    @if (sprints().length > 0) {
      <div class="flex shrink-0 items-center gap-2">
        <app-form-select
          class="w-52 text-sm [&_.nept-form-control]:mb-0"
          label=""
          i18n-placeholder="Placeholder in the sprint picker on a backlog row"
          placeholder="Assign to sprint..."
          [value]="selectedSprintId() ?? null"
          (changed)="onSprintSelected($event)">
          @for (sprint of sprints(); track sprint.id) {
            <app-form-select-option [value]="sprint.id">
              {{ sprint.name }}
            </app-form-select-option>
          }
        </app-form-select>
        <button
          app-flat-button
          color="primary"
          type="button"
          [disabled]="loading() || !selectedSprintId()"
          (click)="onAssign()">
          <span i18n="Button that adds the task to the chosen sprint">Add</span>
        </button>
      </div>
    }
  `,
})
export class SprintBacklogTaskRowComponent {
  readonly task = input.required<TaskViewModel>();
  readonly sprints = input.required<SprintViewModel[]>();
  private readonly sprintCommands = inject(SprintCommandsService);

  readonly loading = this.sprintCommands.isUpdating;

  readonly selectedSprintId = signal<number | undefined>(undefined);

  onSprintSelected(sprintId: number) {
    this.selectedSprintId.set(sprintId);
  }

  onAssign() {
    const taskId = this.task().id;
    const sprintId = this.selectedSprintId();
    if (!taskId || !sprintId) return;

    this.sprintCommands.addTask(sprintId, taskId);
    this.selectedSprintId.set(undefined);
  }
}
