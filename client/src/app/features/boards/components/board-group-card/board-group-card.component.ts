import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { Selected } from '@core/models/selected';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { CardComponent } from '@static/components/card/card.component';

import { LucideCheck, LucideFlag } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { NgClass } from '@angular/common';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityCardColors,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { SprintStatus } from '@core/enums/sprint-status';

@Component({
  selector: 'app-board-group-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    AvatarComponent,
    TaskScopeIdComponent,
    LucideFlag,
    LucideCheck,
    NgClass,
    TooltipDirective,
  ],
  template: `<app-card
    class="mb-[.3rem] flex cursor-pointer flex-col items-start overflow-hidden p-2! text-[14px] tracking-[.1px]"
    [class.selected]="task().selected"
    [ngClass]="priorityClasses()">
    <div class="mb-0 leading-[1.4rem]">{{ task().name }}</div>

    <div class="mt-4 flex flex-row flex-wrap">
      @if (task().sprintName) {
        <div
          class="my-[.2rem] mr-[.2rem] ml-0 rounded-[4px] px-[.4rem] py-[.2rem] text-xs font-medium"
          [class.bg-green-100]="task().sprintStatus === sprintStatus.active"
          [class.text-green-800]="task().sprintStatus === sprintStatus.active"
          [class.bg-neutral-100]="task().sprintStatus !== sprintStatus.active"
          [class.text-neutral-700]="task().sprintStatus !== sprintStatus.active">
          {{ task().sprintName }}
        </div>
      }

      @for (tag of task().tags; track tag) {
        <div
          class="bg-primary/10 my-[.2rem] mr-[.2rem] ml-0 rounded-[4px] px-[.4rem] py-[.2rem]">
          {{ tag }}
        </div>
      }
    </div>

    <div class="mt-2 flex w-full flex-row items-center justify-between">
      <div class="flex items-center gap-2">
        <app-task-scope-id [id]="task().systemId" />

        @if (task().status === 1) {
          <svg lucideCheck class="text-green-500">done</svg>
        }
      </div>

      <div class="flex items-center gap-4">
        @if (estimateLabel()) {
          <span
            class="rounded bg-neutral-100 px-1.5 py-0.5 text-xs font-semibold text-neutral-600 dark:bg-neutral-800 dark:text-neutral-300">
            {{ estimateLabel() }}
          </span>
        }

        @if (priorityVisible()) {
          <span
            class="flex items-center gap-1 text-xs font-medium"
            [ngClass]="priorityColor()"
            [title]="priorityLabel()"
            [appTooltip]="priorityLabel()">
            <svg lucideFlag class="h-5 w-5" [ngClass]="priorityColor()"></svg>
          </span>
        }

        @for (assignee of task().assignees; track assignee.id) {
          <app-avatar
            size="sm"
            class="task-card-user-chip"
            [name]="assignee.displayName"
            [imageUrl]="assignee.pictureUrl">
          </app-avatar>
        }
      </div>
    </div>
  </app-card> `,
})
export class BoardGroupCardComponent {
  readonly task = input.required<Selected<BoardViewTask>>();
  readonly groupId = input.required<number>();
  readonly sprintStatus = SprintStatus;
  readonly priority = computed(() => this.task().priority);

  priorityVisible = computed(() => {
    const p = this.priority();
    return p !== null && p !== undefined && p !== TaskPriority.none;
  });

  priorityColor = computed(() => {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityColors[p];
  });

  priorityClasses = computed(() => {
    const p = this.priority() ?? TaskPriority.none;
    return { [taskPriorityCardColors[p]]: true };
  });

  priorityLabel = computed(() => {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityLabels[p];
  });

  estimateLabel = computed(() => {
    const { estimateType, estimateValue } = this.task();
    if (estimateValue == null) return null;
    return formatEstimate(
      estimateType ?? EstimateType.storyPoints,
      estimateValue
    );
  });
}
