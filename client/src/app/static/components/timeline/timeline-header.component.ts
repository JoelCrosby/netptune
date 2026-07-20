import { Component, computed, input, output } from '@angular/core';
import {
  clippedRangeLeft,
  clippedRangeWidth,
  timelineGridBackground,
  timelineGridBackgroundSize,
} from './timeline-date-geometry';
import { TimelineDateMarkerComponent } from './timeline-date-marker.component';
import { TimelineWeekendShadingComponent } from './timeline-weekend-shading.component';
import {
  TimelineHeaderGroup,
  TimelineRange,
  TimelineTick,
} from './timeline.models';

@Component({
  selector: 'app-timeline-header',
  imports: [TimelineDateMarkerComponent, TimelineWeekendShadingComponent],
  host: { class: 'block sticky top-0 z-20' },
  template: `
    <div class="border-border bg-card flex h-20 border-b">
      <div
        class="border-border bg-card sticky left-0 z-30 flex shrink-0 items-center border-r px-4 font-semibold"
        [style.width.px]="itemColumnWidth()">
        {{ itemLabel() }}
        @if (itemColumnResizable()) {
          <div
            role="separator"
            tabindex="0"
            aria-orientation="vertical"
            class="group absolute top-0 right-0 z-10 flex h-screen w-3 translate-x-1/2 cursor-col-resize touch-none items-center justify-center outline-none"
            [attr.aria-label]="'Resize ' + itemLabel() + ' column'"
            [attr.aria-valuemin]="itemColumnMinWidth()"
            [attr.aria-valuemax]="itemColumnMaxWidth()"
            [attr.aria-valuenow]="itemColumnWidth()"
            (pointerdown)="startColumnResize($event)"
            (pointermove)="moveColumnResize($event)"
            (pointerup)="finishColumnResize($event)"
            (pointercancel)="finishColumnResize($event)"
            (keydown)="resizeColumnWithKeyboard($event)">
            <span
              class="bg-border group-hover:bg-primary group-focus-visible:bg-primary h-full w-px transition-colors"></span>
          </div>
        }
      </div>
      <div
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
        @for (group of headerGroups(); track group.id) {
          <div
            class="border-border/70 bg-card/75 absolute top-0 h-7 overflow-hidden border-r border-b px-2 text-xs leading-7 font-medium whitespace-nowrap"
            [style.left.px]="group.left"
            [style.width.px]="group.width"
            [title]="group.label">
            {{ group.label }}
          </div>
        }
        @for (tick of ticks(); track tick.date) {
          <span
            class="text-muted-foreground absolute top-8 text-xs whitespace-nowrap"
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
  readonly itemColumnResizable = input(false);
  readonly itemColumnMinWidth = input(200);
  readonly itemColumnMaxWidth = input(640);
  readonly itemColumnWidthChanged = output<number>();
  readonly canvasWidth = input.required<number>();
  readonly dayWidth = input.required<number>();
  readonly majorIntervalDays = input(1);
  readonly highlightDate = input<string>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly ticks = input<TimelineTick[]>([]);
  readonly ranges = input<TimelineRange[]>([]);
  readonly headerGroups = input<TimelineHeaderGroup[]>([]);
  readonly gridBackground = timelineGridBackground;
  readonly gridBackgroundSize = computed(() =>
    timelineGridBackgroundSize(this.dayWidth(), this.majorIntervalDays())
  );
  private columnResize?: { originX: number; originWidth: number };

  startColumnResize(event: PointerEvent): void {
    if (event.button !== 0) {
      return;
    }

    event.preventDefault();
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    this.columnResize = {
      originX: event.clientX,
      originWidth: this.itemColumnWidth(),
    };
  }

  moveColumnResize(event: PointerEvent): void {
    const resize = this.columnResize;

    if (!resize) {
      return;
    }

    const width = resize.originWidth + event.clientX - resize.originX;
    this.itemColumnWidthChanged.emit(this.clampColumnWidth(width));
  }

  finishColumnResize(event: PointerEvent): void {
    this.columnResize = undefined;
    const target = event.currentTarget as HTMLElement;

    if (target.hasPointerCapture(event.pointerId)) {
      target.releasePointerCapture(event.pointerId);
    }
  }

  resizeColumnWithKeyboard(event: KeyboardEvent): void {
    const step = event.shiftKey ? 32 : 16;
    const width =
      event.key === 'ArrowLeft'
        ? this.itemColumnWidth() - step
        : event.key === 'ArrowRight'
          ? this.itemColumnWidth() + step
          : event.key === 'Home'
            ? this.itemColumnMinWidth()
            : event.key === 'End'
              ? this.itemColumnMaxWidth()
              : undefined;

    if (width === undefined) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    this.itemColumnWidthChanged.emit(this.clampColumnWidth(width));
  }

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

  private clampColumnWidth(width: number): number {
    return Math.min(
      this.itemColumnMaxWidth(),
      Math.max(this.itemColumnMinWidth(), width)
    );
  }
}
