import { Component, computed, input, output } from '@angular/core';
import {
  addDays,
  dateLabel,
  inclusiveDayCount,
  timelineDayWidth,
  todayDate,
} from '@static/components/timeline/timeline-date-geometry';
import { TimelineHeaderComponent } from '@static/components/timeline/timeline-header.component';
import { TimelineLaneComponent } from '@static/components/timeline/timeline-lane.component';
import {
  TimelineRange,
  TimelineTick,
  TimelineZoom,
} from '@static/components/timeline/timeline.models';
import { RoadmapTask, RoadmapViewModel } from '../models/roadmap.models';
import { buildRoadmapGroups } from '../utils/roadmap-row-builder';
import { RoadmapTaskRowComponent } from './roadmap-task-row.component';

const taskColumnWidth = 320;

@Component({
  selector: 'app-roadmap-timeline',
  imports: [
    TimelineHeaderComponent,
    TimelineLaneComponent,
    RoadmapTaskRowComponent,
  ],
  host: { class: 'block min-h-0 flex-1' },
  template: `
    <div
      class="custom-scroll bg-card h-full overflow-auto"
      role="region"
      aria-label="Task timeline">
      <div [style.width.px]="totalWidth()" class="relative min-h-full">
        <app-timeline-header
          itemLabel="Task"
          [itemColumnWidth]="taskColumnWidth"
          [canvasWidth]="canvasWidth()"
          [dayWidth]="dayWidth()"
          [majorIntervalDays]="majorIntervalDays()"
          [highlightDate]="today"
          [from]="from()"
          [to]="to()"
          [ticks]="ticks()"
          [ranges]="sprintRanges()" />

        @for (group of groups(); track group.id) {
          <div class="border-border bg-muted/30 flex h-9 border-b">
            <div
              class="border-border bg-muted/5 sticky left-0 z-10 flex shrink-0 items-center border-r px-4 text-sm font-semibold"
              [style.width.px]="taskColumnWidth">
              {{ group.name }}
            </div>
            <app-timeline-lane
              [canvasWidth]="canvasWidth()"
              [dayWidth]="dayWidth()"
              [majorIntervalDays]="majorIntervalDays()"
              [highlightDate]="today"
              [from]="from()"
              [to]="to()" />
          </div>

          @for (row of group.tasks; track row.task.id) {
            <app-roadmap-task-row
              [row]="row"
              [taskColumnWidth]="taskColumnWidth"
              [canvasWidth]="canvasWidth()"
              [dayWidth]="dayWidth()"
              [majorIntervalDays]="majorIntervalDays()"
              [highlightDate]="today"
              [from]="from()"
              [to]="to()"
              (taskSelected)="taskSelected.emit($event)" />
          }
        } @empty {
          <div class="text-muted-foreground p-12 text-center text-sm">
            No scheduled tasks overlap this range.
          </div>
        }
      </div>
    </div>
  `,
})
export class RoadmapTimelineComponent {
  readonly view = input.required<RoadmapViewModel>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly zoom = input.required<TimelineZoom>();
  readonly taskSelected = output<RoadmapTask>();

  readonly taskColumnWidth = taskColumnWidth;
  readonly today = todayDate();
  readonly dayWidth = computed(() => timelineDayWidth(this.zoom()));
  readonly majorIntervalDays = computed(() =>
    this.zoom() === 'day' ? 1 : this.zoom() === 'week' ? 7 : 30
  );
  readonly canvasWidth = computed(
    () =>
      Math.max(1, inclusiveDayCount(this.from(), this.to())) * this.dayWidth()
  );
  readonly totalWidth = computed(() => taskColumnWidth + this.canvasWidth());
  readonly groups = computed(() =>
    buildRoadmapGroups(this.view().tasks, this.view().relations)
  );
  readonly sprintRanges = computed<TimelineRange[]>(() =>
    this.view().sprints.map((sprint) => ({
      id: sprint.id,
      label: sprint.name,
      startDate: sprint.startDate,
      endDate: sprint.endDate,
    }))
  );
  readonly ticks = computed<TimelineTick[]>(() => {
    const stride = this.majorIntervalDays();
    const count = Math.max(0, inclusiveDayCount(this.from(), this.to()));
    const ticks: TimelineTick[] = [];

    for (let offset = 0; offset < count; offset += stride) {
      const date = addDays(this.from(), offset);
      ticks.push({
        date,
        label: dateLabel(date, this.zoom()),
        left: offset * this.dayWidth() + 4,
      });
    }

    return ticks;
  });
}
