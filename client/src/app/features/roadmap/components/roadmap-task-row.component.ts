import { Component, input, output } from '@angular/core';
import { LucideChevronDown, LucideChevronRight } from '@lucide/angular';
import { TimelineBarComponent } from '@static/components/timeline/timeline-bar.component';
import { TimelineLaneComponent } from '@static/components/timeline/timeline-lane.component';
import { TimelineSchedule } from '@static/components/timeline/timeline.models';
import {
  RoadmapDisplayTask,
  RoadmapScheduleChange,
  RoadmapTask,
} from '../models/roadmap.models';

@Component({
  selector: 'app-roadmap-task-row',
  imports: [
    LucideChevronDown,
    LucideChevronRight,
    TimelineBarComponent,
    TimelineLaneComponent,
  ],
  styles: `
    :host {
      content-visibility: auto;
      contain-intrinsic-block-size: 44px;
    }
  `,
  host: { class: 'block' },
  template: `
    <div class="border-border flex h-11 border-b">
      <div
        class="border-border bg-card sticky left-0 z-12 flex shrink-0 items-center overflow-hidden border-r"
        [style.width.px]="taskColumnWidth()"
        [style.padding-left.px]="8 + row().depth * 18">
        @if (row().hasChildren) {
          <button
            type="button"
            class="hover:bg-muted/10 flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded"
            [attr.aria-label]="collapseLabel()"
            [attr.aria-expanded]="!collapsed()"
            (click)="collapseToggled.emit(row().task.id)">
            @if (collapsed()) {
              <svg lucideChevronRight class="h-4 w-4"></svg>
            } @else {
              <svg lucideChevronDown class="h-4 w-4"></svg>
            }
          </button>
        } @else {
          <span class="w-7 shrink-0"></span>
        }

        <button
          type="button"
          class="hover:bg-muted/10 flex min-w-0 flex-1 cursor-pointer items-center gap-2 self-stretch overflow-hidden px-2 text-left"
          [title]="row().task.systemId + ' ' + row().task.name"
          (click)="taskSelected.emit(row().task)">
          <span class="text-muted w-14 shrink-0 truncate text-xs">
            {{ row().task.systemId }}
          </span>
          <span class="truncate text-sm">{{ row().task.name }}</span>
        </button>

        @if (row().blockedByCount > 0) {
          <span
            class="ml-1 shrink-0 rounded bg-red-500/10 px-1.5 py-0.5 text-[10px] text-red-600 dark:text-red-400"
            [title]="blockerLabel()">
            {{ row().blockedByCount }}
          </span>
        }
        @if (row().offscreenBlockedByCount > 0) {
          <span
            class="ml-1 shrink-0 rounded border border-red-500/30 px-1 py-0.5 text-[10px] text-red-600 dark:text-red-400"
            [title]="offscreenBlockerLabel()">
            ↗ {{ row().offscreenBlockedByCount }}
          </span>
        }
      </div>

      <app-timeline-lane
        [canvasWidth]="canvasWidth()"
        [dayWidth]="dayWidth()"
        [majorIntervalDays]="majorIntervalDays()"
        [highlightDate]="highlightDate()"
        [from]="from()"
        [to]="to()">
        <app-timeline-bar
          [label]="row().task.name"
          [accessibleLabel]="taskAriaLabel(row().task)"
          [startDate]="row().task.startDate"
          [endDate]="row().task.dueDate"
          [from]="from()"
          [to]="to()"
          [dayWidth]="dayWidth()"
          [editable]="editable()"
          [busy]="busy()"
          (scheduleChanged)="changeSchedule($event)"
          (activated)="taskSelected.emit(row().task)" />
      </app-timeline-lane>
    </div>
  `,
})
export class RoadmapTaskRowComponent {
  readonly row = input.required<RoadmapDisplayTask>();
  readonly taskColumnWidth = input.required<number>();
  readonly canvasWidth = input.required<number>();
  readonly dayWidth = input.required<number>();
  readonly majorIntervalDays = input.required<number>();
  readonly highlightDate = input.required<string>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly collapsed = input(false);
  readonly editable = input(false);
  readonly busy = input(false);
  readonly taskSelected = output<RoadmapTask>();
  readonly collapseToggled = output<number>();
  readonly scheduleChanged = output<RoadmapScheduleChange>();

  changeSchedule(schedule: TimelineSchedule): void {
    this.scheduleChanged.emit({ task: this.row().task, schedule });
  }

  collapseLabel(): string {
    return `${this.collapsed() ? 'Expand' : 'Collapse'} ${this.row().task.systemId}`;
  }

  blockerLabel(): string {
    const count = this.row().blockedByCount;
    return `${count} blocker${count === 1 ? '' : 's'}`;
  }

  offscreenBlockerLabel(): string {
    const count = this.row().offscreenBlockedByCount;
    return `${count} blocker${count === 1 ? '' : 's'} outside the current view`;
  }

  taskAriaLabel(task: RoadmapTask): string {
    const start = task.startDate ? `starts ${task.startDate}` : 'no start date';
    const due = task.dueDate ? `due ${task.dueDate}` : 'no due date';
    const instructions = this.editable()
      ? '. Arrow keys move; Shift plus Arrow resizes the due date; Alt plus Arrow resizes the start date'
      : '';
    return `${task.systemId} ${task.name}, ${start}, ${due}${instructions}`;
  }
}
