import { NgClass } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityCardColors,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { Selected } from '@core/models/selected';
import { StatusCategory } from '@core/models/status';
import { BoardViewTask } from '@core/models/view-models/board-view';
import {
  LucideCheck,
  LucideFlag,
  LucideMessageSquareText,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';

@Component({
  selector: 'app-board-group-card',
  imports: [
    AvatarComponent,
    BadgeComponent,
    TaskScopeIdComponent,
    LucideFlag,
    LucideCheck,
    LucideMessageSquareText,
    NgClass,
    TooltipDirective,
    SprintBadgeComponent,
  ],
  template: `<div
    class="border-border bg-board-group-card mb-[.3rem] flex min-h-24 flex-col items-start overflow-hidden rounded-sm border p-2! text-[14px] tracking-[.1px] shadow-sm"
    [class.bg-primary/25]="task().selected"
    [class.border-bg-primary]="task().selected"
    [ngClass]="priorityClasses()">
    <div class="mb-0 leading-[1.4rem] select-none">{{ task().name }}</div>

    <div class="mt-4 flex flex-row flex-wrap">
      @if (task().sprintName) {
        <app-sprint-badge
          class="my-[.2rem] mr-[.2rem] ml-0"
          [name]="task().sprintName!"
          [status]="task().sprintStatus" />
      }

      @for (tag of task().tags; track tag) {
        <div
          class="bg-primary/10 my-[.2rem] mr-[.2rem] ml-0 rounded-[4px] px-[.4rem] py-[.2rem] select-none">
          {{ tag }}
        </div>
      }
    </div>

    <div class="mt-2 flex w-full flex-row items-center justify-between">
      <div class="flex items-center gap-2">
        <app-task-scope-id [id]="task().systemId" />

        @if (task().hasComments) {
          <svg
            lucideMessageSquareText
            class="text-muted h-4 w-4"
            aria-label="Has comments"
            appTooltip="Has comments"></svg>
        }

        @if (task().statusCategory === statusCategory.done) {
          <svg lucideCheck class="text-green-500">done</svg>
        }
      </div>

      <div class="flex items-center gap-4">
        @if (estimateLabel()) {
          <app-badge shape="rounded">
            {{ estimateLabel() }}
          </app-badge>
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

        <div class="flex items-center gap-1">
          @for (assignee of task().assignees; track assignee.id) {
            <app-avatar
              size="sm"
              class="task-card-user-chip"
              [name]="assignee.displayName"
              [imageUrl]="assignee.pictureUrl"
              [isServiceAccount]="assignee.isServiceAccount ?? false">
            </app-avatar>
          }
        </div>
      </div>
    </div>
  </div> `,
})
export class BoardGroupCardComponent {
  readonly task = input.required<Selected<BoardViewTask>>();
  readonly groupId = input.required<number>();
  readonly statusCategory = StatusCategory;
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
