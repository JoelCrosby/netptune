import { NgClass } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
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
import { BoardSelectionService } from '@core/services/board-selection.service';
import { PERMISSIONS } from '@app/core/auth/permissions';
import {
  LucideCheck,
  LucideFlag,
  LucideMessageSquareText,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskFlagBadgeComponent } from '@static/components/task-flag-badge.component';
import { SelectionCheckboxComponent } from '@static/components/checkbox/selection-checkbox.component';

@Component({
  selector: 'app-board-group-card',
  styles: [
    `
      @keyframes selection-checkbox-in {
        from {
          opacity: 0;
          transform: scale(0.85);
        }
        to {
          opacity: 1;
          transform: scale(1);
        }
      }

      .selection-checkbox {
        animation: selection-checkbox-in 140ms ease-out;
      }

      @media (prefers-reduced-motion: reduce) {
        .selection-checkbox {
          animation: none;
        }
      }
    `,
  ],
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
    TaskFlagBadgeComponent,
    SelectionCheckboxComponent,
  ],
  template: `
    <div
      class="border-border bg-board-group-card relative mb-[.3rem] flex min-h-24 flex-col items-start overflow-hidden rounded-sm border p-2! text-[14px] tracking-[.1px] shadow-sm"
      [ngClass]="cardClasses()">
      @if (selectionActive()) {
        <div
          class="selection-checkbox absolute top-1 right-1 z-10 flex items-center rounded-[4px] p-1"
          (click)="onCheckboxClicked($event)">
          <span
            class="bg-board-group-card absolute inset-0 rounded-[4px]"></span>
          <span
            class="absolute inset-0 rounded-[4px]"
            [ngClass]="overlayClasses()"></span>

          <app-selection-checkbox
            class="relative"
            [checked]="!!task().selected"
            i18n-label="Accessible label for the board card selection checkbox"
            label="Select task" />
        </div>
      }

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
              i18n-aria-label="
                Accessible label for the icon marking a task that has comments
              "
              aria-label="Has comments"
              i18n-appTooltip="
                Tooltip on the icon marking a task that has comments
              "
              appTooltip="Has comments"></svg>
          }

          @if (readFlags()) {
            <app-task-flag-badge [count]="task().flagCount" />
          }

          @if (task().statusCategory === statusCategory.done) {
            <svg lucideCheck class="text-green-500"></svg>
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
                [isServiceAccount]="
                  assignee.isServiceAccount ?? false
                "></app-avatar>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class BoardGroupCardComponent {
  private readonly selection = inject(BoardSelectionService);

  readonly selectionActive = computed(() => this.selection.count() > 0);
  readonly task = input.required<Selected<BoardViewTask>>();
  readonly groupId = input.required<number>();
  readonly statusCategory = StatusCategory;
  readonly priority = computed(() => this.task().priority);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);

  priorityVisible = computed(() => {
    const p = this.priority();
    return p !== null && p !== undefined && p !== TaskPriority.none;
  });

  priorityColor = computed(() => {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityColors[p];
  });

  // The priority tint uses important utilities, so a selected card has to drop
  // them entirely rather than try to layer the selection colour on top.
  cardClasses = computed(() => {
    if (this.task().selected) {
      return 'bg-primary/25! border-primary!';
    }

    return taskPriorityCardColors[this.priority() ?? TaskPriority.none];
  });

  // The checkbox sits over the card content, so it carries the same background
  // layers as the card itself to keep the text behind it masked.
  overlayClasses = computed(() => {
    if (this.task().selected) {
      return 'bg-primary/25';
    }

    return taskPriorityCardColors[this.priority() ?? TaskPriority.none];
  });

  onCheckboxClicked(event: MouseEvent) {
    event.stopPropagation();

    if (this.task().selected) {
      this.selection.deselect(this.task().id);
    } else {
      this.selection.select(this.task().id);
    }
  }

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
