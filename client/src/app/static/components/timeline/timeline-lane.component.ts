import { Component, computed, input } from '@angular/core';
import {
  timelineGridBackground,
  timelineGridBackgroundSize,
} from './timeline-date-geometry';
import { TimelineDateMarkerComponent } from './timeline-date-marker.component';
import { TimelineWeekendShadingComponent } from './timeline-weekend-shading.component';

@Component({
  selector: 'app-timeline-lane',
  imports: [TimelineDateMarkerComponent, TimelineWeekendShadingComponent],
  host: { class: 'block relative h-full' },
  template: `<div
    class="relative h-full"
    [style.width.px]="canvasWidth()"
    [style.background-image]="gridBackground"
    [style.background-size]="gridBackgroundSize()">
    <app-timeline-weekend-shading [from]="from()" [dayWidth]="dayWidth()" />
    @if (highlightDate(); as date) {
      <app-timeline-date-marker
        [date]="date"
        [from]="from()"
        [to]="to()"
        [dayWidth]="dayWidth()" />
    }
    <ng-content />
  </div>`,
})
export class TimelineLaneComponent {
  readonly canvasWidth = input.required<number>();
  readonly dayWidth = input.required<number>();
  readonly majorIntervalDays = input(1);
  readonly highlightDate = input<string>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly gridBackground = timelineGridBackground;
  readonly gridBackgroundSize = computed(() =>
    timelineGridBackgroundSize(this.dayWidth(), this.majorIntervalDays())
  );
}
