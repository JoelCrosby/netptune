import { Component, input, output } from '@angular/core';
import { TimelineBarComponent } from '@static/components/timeline/timeline-bar.component';
import { TimelineLaneComponent } from '@static/components/timeline/timeline-lane.component';
import { RoadmapDisplayTask, RoadmapTask } from '../models/roadmap.models';

@Component({
  selector: 'app-roadmap-task-row',
  imports: [TimelineBarComponent, TimelineLaneComponent],
  host: { class: 'block' },
  template: `
    <div class="border-border flex h-11 border-b">
      <button
        type="button"
        class="border-border bg-card hover:bg-muted/10 sticky left-0 z-10 flex shrink-0 cursor-pointer items-center gap-3 overflow-hidden border-r px-4 text-left"
        [style.width.px]="taskColumnWidth()"
        [style.padding-left.px]="16 + row().depth * 18"
        [title]="row().task.systemId + ' ' + row().task.name"
        (click)="taskSelected.emit(row().task)">
        <span class="text-muted-foreground w-20 shrink-0 text-xs">
          {{ row().task.systemId }}
        </span>
        <span class="truncate text-sm">{{ row().task.name }}</span>
        @if (row().blockedByCount > 0) {
          <span
            class="ml-auto shrink-0 rounded bg-red-500/10 px-1.5 py-0.5 text-[10px] text-red-600 dark:text-red-400">
            {{ row().blockedByCount }}
            blocker{{ row().blockedByCount === 1 ? '' : 's' }}
          </span>
        }
      </button>

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
  readonly taskSelected = output<RoadmapTask>();

  taskAriaLabel(task: RoadmapTask): string {
    const start = task.startDate ? `starts ${task.startDate}` : 'no start date';
    const due = task.dueDate ? `due ${task.dueDate}` : 'no due date';
    return `${task.systemId} ${task.name}, ${start}, ${due}`;
  }
}
