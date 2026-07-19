import { Component, computed, input } from '@angular/core';
import {
  clippedRangeLeft,
  clippedRangeWidth,
  timelineGridBackground,
  timelineGridBackgroundSize,
} from './timeline-date-geometry';
import { TimelineDateMarkerComponent } from './timeline-date-marker.component';
import { TimelineRange, TimelineTick } from './timeline.models';

@Component({
  selector: 'app-timeline-header',
  imports: [TimelineDateMarkerComponent],
  host: { class: 'block sticky top-0 z-20' },
  template: `
    <div class="border-border bg-card flex h-14 border-b">
      <div
        class="border-border bg-card sticky left-0 z-30 flex shrink-0 items-center border-r px-4 font-semibold"
        [style.width.px]="itemColumnWidth()">
        {{ itemLabel() }}
      </div>
      <div
        class="relative h-full"
        [style.width.px]="canvasWidth()"
        [style.background-image]="gridBackground"
        [style.background-size]="gridBackgroundSize()">
        @if (highlightDate(); as date) {
          <app-timeline-date-marker
            [date]="date"
            [from]="from()"
            [to]="to()"
            [dayWidth]="dayWidth()" />
        }
        @for (tick of ticks(); track tick.date) {
          <span
            class="text-muted-foreground absolute top-2 text-xs whitespace-nowrap"
            [style.left.px]="tick.left">
            {{ tick.label }}
          </span>
        }
        @for (range of ranges(); track range.id) {
          <div
            class="absolute bottom-1 h-5 overflow-hidden rounded bg-violet-500/15 px-2 text-xs leading-5 whitespace-nowrap text-violet-700 dark:text-violet-300"
            [style.left.px]="rangeLeft(range.startDate)"
            [style.width.px]="rangeWidth(range.startDate, range.endDate)"
            [title]="range.label">
            {{ range.label }}
          </div>
        }
      </div>
    </div>
  `,
})
export class TimelineHeaderComponent {
  readonly itemLabel = input('Item');
  readonly itemColumnWidth = input.required<number>();
  readonly canvasWidth = input.required<number>();
  readonly dayWidth = input.required<number>();
  readonly majorIntervalDays = input(1);
  readonly highlightDate = input<string>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly ticks = input<TimelineTick[]>([]);
  readonly ranges = input<TimelineRange[]>([]);
  readonly gridBackground = timelineGridBackground;
  readonly gridBackgroundSize = computed(() =>
    timelineGridBackgroundSize(this.dayWidth(), this.majorIntervalDays())
  );

  rangeLeft(start: string): number {
    return clippedRangeLeft(this.from(), start.slice(0, 10), this.dayWidth());
  }

  rangeWidth(start: string, end: string): number {
    return clippedRangeWidth(
      this.from(),
      this.to(),
      start.slice(0, 10),
      end.slice(0, 10),
      this.dayWidth()
    );
  }
}
