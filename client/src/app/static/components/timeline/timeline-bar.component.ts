import { Component, computed, input, output, signal } from '@angular/core';
import {
  addDays,
  clippedRangeLeft,
  clippedRangeWidth,
} from './timeline-date-geometry';
import { TimelineSchedule } from './timeline.models';

type TimelineInteraction = 'move' | 'resize-start' | 'resize-end';

interface PointerInteraction {
  mode: TimelineInteraction;
  originX: number;
  schedule: TimelineSchedule;
}

@Component({
  selector: 'app-timeline-bar',
  host: { class: 'contents' },
  template: `
    @if (milestone()) {
      <button
        type="button"
        class="border-primary-foreground/60 bg-primary focus-visible:ring-primary-foreground focus-visible:ring-offset-card absolute top-3 h-5 w-5 rotate-45 rounded-sm border shadow focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
        [class.cursor-grab]="editable() && !busy()"
        [class.cursor-pointer]="!editable()"
        [class.opacity-60]="busy()"
        [class.touch-none]="editable()"
        [style.left.px]="left()"
        [attr.aria-label]="accessibleLabel()"
        [attr.aria-busy]="busy()"
        (click)="activate($event)"
        (keydown)="handleKeydown($event)"
        (pointerdown)="startPointerInteraction($event, 'move')"
        (pointermove)="movePointerInteraction($event)"
        (pointerup)="finishPointerInteraction($event)"
        (pointercancel)="cancelPointerInteraction()"></button>
    } @else {
      <button
        type="button"
        class="bg-primary text-primary-foreground hover:bg-primary/90 focus-visible:ring-primary-foreground focus-visible:ring-offset-card absolute top-2 h-7 min-w-2 overflow-hidden rounded px-2 text-left text-xs leading-7 shadow focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
        [class.cursor-grab]="editable() && !busy()"
        [class.cursor-pointer]="!editable()"
        [class.opacity-60]="busy()"
        [class.touch-none]="editable()"
        [style.left.px]="left()"
        [style.width.px]="width()"
        [attr.aria-label]="accessibleLabel()"
        [attr.aria-busy]="busy()"
        (click)="activate($event)"
        (keydown)="handleKeydown($event)"
        (pointerdown)="startPointerInteraction($event, 'move')"
        (pointermove)="movePointerInteraction($event)"
        (pointerup)="finishPointerInteraction($event)"
        (pointercancel)="cancelPointerInteraction()">
        <span class="whitespace-nowrap">{{ label() }}</span>
      </button>
    }

    @if (editable() && !busy()) {
      <span
        class="bg-primary-foreground/35 hover:bg-primary-foreground/60 absolute top-2 z-10 h-7 w-1.5 cursor-ew-resize touch-none rounded-l"
        [style.left.px]="left() - 3"
        title="Resize start date"
        aria-hidden="true"
        (pointerdown)="startPointerInteraction($event, 'resize-start')"
        (pointermove)="movePointerInteraction($event)"
        (pointerup)="finishPointerInteraction($event)"
        (pointercancel)="cancelPointerInteraction()"></span>
      <span
        class="bg-primary-foreground/35 hover:bg-primary-foreground/60 absolute top-2 z-10 h-7 w-1.5 cursor-ew-resize touch-none rounded-r"
        [style.left.px]="left() + width() - 3"
        title="Resize due date"
        aria-hidden="true"
        (pointerdown)="startPointerInteraction($event, 'resize-end')"
        (pointermove)="movePointerInteraction($event)"
        (pointerup)="finishPointerInteraction($event)"
        (pointercancel)="cancelPointerInteraction()"></span>
    }
  `,
})
export class TimelineBarComponent {
  readonly label = input.required<string>();
  readonly accessibleLabel = input.required<string>();
  readonly startDate = input<string | null>();
  readonly endDate = input<string | null>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly dayWidth = input.required<number>();
  readonly editable = input(false);
  readonly busy = input(false);
  readonly activated = output();
  readonly scheduleChanged = output<TimelineSchedule>();

  private readonly preview = signal<TimelineSchedule | undefined>(undefined);
  private pointerInteraction: PointerInteraction | undefined;
  private suppressActivation = false;

  readonly schedule = computed<TimelineSchedule>(() => {
    return (
      this.preview() ?? {
        startDate: this.startDate() ?? null,
        endDate: this.endDate() ?? null,
      }
    );
  });

  readonly milestone = computed(() => {
    return !this.schedule().startDate && !!this.schedule().endDate;
  });

  readonly left = computed(() => {
    const schedule = this.schedule();
    const start = schedule.startDate ?? schedule.endDate ?? this.from();
    return clippedRangeLeft(this.from(), start, this.dayWidth());
  });

  readonly width = computed(() => {
    const schedule = this.schedule();
    const start = schedule.startDate ?? schedule.endDate ?? this.from();
    const end = schedule.endDate ?? schedule.startDate ?? start;

    return clippedRangeWidth(
      this.from(),
      this.to(),
      start,
      end,
      this.dayWidth()
    );
  });

  activate(event: MouseEvent): void {
    if (this.suppressActivation) {
      event.preventDefault();
      this.suppressActivation = false;
      return;
    }

    this.activated.emit();
  }

  startPointerInteraction(
    event: PointerEvent,
    mode: TimelineInteraction
  ): void {
    if (!this.editable() || this.busy() || event.button !== 0) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);

    this.pointerInteraction = {
      mode,
      originX: event.clientX,
      schedule: this.schedule(),
    };
  }

  movePointerInteraction(event: PointerEvent): void {
    const interaction = this.pointerInteraction;

    if (!interaction) {
      return;
    }

    const dayDelta = Math.round(
      (event.clientX - interaction.originX) / this.dayWidth()
    );
    this.preview.set(
      adjustSchedule(interaction.schedule, interaction.mode, dayDelta)
    );
  }

  finishPointerInteraction(event: PointerEvent): void {
    const interaction = this.pointerInteraction;

    if (!interaction) {
      return;
    }

    const schedule = this.preview() ?? interaction.schedule;
    const changed = !sameSchedule(schedule, interaction.schedule);
    this.pointerInteraction = undefined;
    this.preview.set(undefined);

    if (changed) {
      this.suppressActivation = true;
      this.scheduleChanged.emit(schedule);
      setTimeout(() => {
        this.suppressActivation = false;
      });
    }

    const target = event.currentTarget as HTMLElement;

    if (target.hasPointerCapture(event.pointerId)) {
      target.releasePointerCapture(event.pointerId);
    }
  }

  cancelPointerInteraction(): void {
    this.pointerInteraction = undefined;
    this.preview.set(undefined);
  }

  handleKeydown(event: KeyboardEvent): void {
    const direction =
      event.key === 'ArrowLeft' ? -1 : event.key === 'ArrowRight' ? 1 : 0;

    if (!this.editable() || this.busy() || direction === 0) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    const mode = event.altKey
      ? 'resize-start'
      : event.shiftKey
        ? 'resize-end'
        : 'move';
    const schedule = adjustSchedule(this.schedule(), mode, direction);

    if (!sameSchedule(schedule, this.schedule())) {
      this.scheduleChanged.emit(schedule);
    }
  }
}

const adjustSchedule = (
  schedule: TimelineSchedule,
  mode: TimelineInteraction,
  dayDelta: number
): TimelineSchedule => {
  if (dayDelta === 0) {
    return schedule;
  }

  if (mode === 'move') {
    return {
      startDate: schedule.startDate
        ? addDays(schedule.startDate, dayDelta)
        : null,
      endDate: schedule.endDate ? addDays(schedule.endDate, dayDelta) : null,
    };
  }

  if (mode === 'resize-start') {
    const currentStart = schedule.startDate ?? schedule.endDate;
    const proposedStart = currentStart ? addDays(currentStart, dayDelta) : null;
    const startDate =
      proposedStart && schedule.endDate && proposedStart > schedule.endDate
        ? schedule.endDate
        : proposedStart;

    return { ...schedule, startDate };
  }

  const currentEnd = schedule.endDate ?? schedule.startDate;
  const proposedEnd = currentEnd ? addDays(currentEnd, dayDelta) : null;
  const endDate =
    proposedEnd && schedule.startDate && proposedEnd < schedule.startDate
      ? schedule.startDate
      : proposedEnd;

  return { ...schedule, endDate };
};

const sameSchedule = (
  left: TimelineSchedule,
  right: TimelineSchedule
): boolean =>
  left.startDate === right.startDate && left.endDate === right.endDate;
