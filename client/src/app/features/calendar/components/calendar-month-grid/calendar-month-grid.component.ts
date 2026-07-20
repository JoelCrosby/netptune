import { Component, computed, input, output, signal } from '@angular/core';
import { Grid, GridCell, GridCellWidget, GridRow } from '@angular/aria/grid';
import { ScheduledTask } from '@core/models/scheduled-task';
import {
  CalendarDay,
  CalendarTaskMove,
  CalendarTaskMoveRequest,
  calendarTaskDragType,
} from '../../models/calendar.models';
import { calendarDayLabel } from '../../utils/calendar-range';
import {
  CalendarTaskLane,
  calendarTaskLanesByDate,
  compareCalendarTasks,
  taskOccursOn,
  taskStartsOn,
} from '../../utils/calendar-tasks';
import { CalendarTaskItemComponent } from '../calendar-task-item/calendar-task-item.component';

const visibleTaskCount = 3;

@Component({
  selector: 'app-calendar-month-grid',
  imports: [CalendarTaskItemComponent, Grid, GridCell, GridCellWidget, GridRow],
  template: `
    <div class="bg-card sticky top-0 z-20 grid grid-cols-7" aria-hidden="true">
      @for (weekday of weekdays; track weekday) {
        <div
          class="border-border bg-muted/10 border-r border-b px-2 py-1.5 text-center text-xs font-semibold last:border-r-0">
          <span class="hidden sm:inline">{{ weekday }}</span>
          <span class="sm:hidden">{{ weekday.slice(0, 1) }}</span>
        </div>
      }
    </div>

    <div
      ngGrid
      focusMode="roving"
      rowWrap="continuous"
      colWrap="continuous"
      aria-label="Task calendar"
      (dragleave)="leaveGrid($event)"
      (dragend)="endTaskDrag()">
      @for (week of weeks(); track week[0].date; let weekIndex = $index) {
        <div ngGridRow class="grid grid-cols-7" [rowIndex]="weekIndex + 1">
          @for (day of week; track day.date; let dayIndex = $index) {
            <div
              ngGridCell
              class="border-border focus-visible:ring-ring hover:bg-primary/5 min-h-28 min-w-0 border-r border-b p-1 transition-colors focus-visible:z-10 focus-visible:ring-2 focus-visible:outline-none sm:min-h-32 sm:p-1.5"
              [class.bg-neutral-100]="!day.currentMonth"
              [class.dark:bg-black/30]="!day.currentMonth"
              [class.ring-primary]="day.today"
              [class.ring-1]="day.today"
              [class.bg-primary/10]="selectedDate() === day.date"
              [class.calendar-drop-target]="dragTargetDate() === day.date"
              [attr.aria-label]="dayLabel(day)"
              [rowIndex]="weekIndex + 1"
              [colIndex]="dayIndex + 1"
              (click)="selectDay(day.date)"
              (dragover)="allowDrop($event, day.date)"
              (drop)="dropTask($event, day.date)">
              <div class="mb-2 flex items-center justify-between">
                <span
                  class="flex h-6 min-w-6 items-center justify-center rounded-full text-xs"
                  [class.bg-primary]="day.today"
                  [class.text-primary-foreground]="day.today"
                  [class.text-muted-foreground]="!day.currentMonth">
                  {{ day.dayNumber }}
                </span>
                @if (dragTargetDate() === day.date) {
                  <span
                    class="bg-primary text-primary-foreground rounded px-1.5 py-0.5 text-[10px] font-semibold"
                    aria-hidden="true">
                    Drop here
                  </span>
                }
              </div>

              <div class="space-y-1 pb-1">
                @for (task of visibleTaskLanes(day.date); track $index) {
                  @if (task) {
                    <app-calendar-task-item
                      [task]="task"
                      [date]="day.date"
                      [dayIndex]="dayIndex"
                      [showLabel]="showTaskLabel(task, day.date, dayIndex)"
                      [editable]="
                        canUpdateTasks() && !pendingTaskIds().has(task.id)
                      "
                      (dragStarted)="startTaskDrag(day.date)"
                      (taskSelected)="taskSelected.emit($event)"
                      (moveRequested)="
                        taskMoveRequested.emit({ task, fromDate: day.date })
                      " />
                  } @else {
                    <div class="h-6" aria-hidden="true"></div>
                  }
                }

                @if (overflowCount(day.date); as overflow) {
                  <button
                    ngGridCellWidget
                    type="button"
                    class="text-muted-foreground hover:text-foreground focus-visible:ring-ring w-full rounded px-1 text-left text-xs font-medium focus-visible:ring-2 focus-visible:outline-none"
                    [attr.aria-label]="showMoreLabel(day.date, overflow)"
                    (click)="selectDay(day.date); $event.stopPropagation()">
                    +{{ overflow }} more
                  </button>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      min-width: 44rem;
    }

    .calendar-drop-target {
      z-index: 1;
      background-color: color-mix(in srgb, var(--primary) 16%, var(--card));
      box-shadow: inset 0 0 0 2px var(--primary);
    }
  `,
})
export class CalendarMonthGridComponent {
  readonly days = input.required<CalendarDay[]>();
  readonly tasks = input<ScheduledTask[]>([]);
  readonly selectedDate = input<string>();
  readonly canUpdateTasks = input(false);
  readonly pendingTaskIds = input<ReadonlySet<number>>(new Set());

  readonly daySelected = output<string>();
  readonly taskSelected = output<ScheduledTask>();
  readonly taskMoved = output<CalendarTaskMove>();
  readonly taskMoveRequested = output<CalendarTaskMoveRequest>();

  protected readonly dragSourceDate = signal<string | undefined>(undefined);
  protected readonly dragTargetDate = signal<string | undefined>(undefined);

  protected readonly weekdays = [
    'Sunday',
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
  ];

  protected readonly weeks = computed(() =>
    Array.from({ length: 6 }, (_, index) =>
      this.days().slice(index * 7, index * 7 + 7)
    )
  );

  private readonly tasksByDate = computed(() => {
    const tasks = [...this.tasks()].sort(compareCalendarTasks);
    return new Map(
      this.days().map((day) => [
        day.date,
        tasks.filter((task) => taskOccursOn(task, day.date)),
      ])
    );
  });

  private readonly taskLanesByDate = computed(() =>
    calendarTaskLanesByDate(
      this.tasks(),
      this.weeks().map((week) => week.map((day) => day.date))
    )
  );

  protected visibleTaskLanes(date: string): CalendarTaskLane[] {
    return (this.taskLanesByDate().get(date) ?? []).slice(0, visibleTaskCount);
  }

  protected overflowCount(date: string): number {
    return (this.taskLanesByDate().get(date) ?? [])
      .slice(visibleTaskCount)
      .filter((task) => !!task).length;
  }

  protected dayLabel(day: CalendarDay): string {
    const count = this.tasksByDate().get(day.date)?.length ?? 0;
    return `${calendarDayLabel(day.date)}, ${count} scheduled ${count === 1 ? 'task' : 'tasks'}`;
  }

  protected showMoreLabel(date: string, count: number): string {
    return `Show ${count} more tasks for ${calendarDayLabel(date)}`;
  }

  protected showTaskLabel(
    task: ScheduledTask,
    date: string,
    dayIndex: number
  ): boolean {
    return dayIndex === 0 || taskStartsOn(task) === date;
  }

  protected selectDay(date: string): void {
    this.daySelected.emit(date);
  }

  protected startTaskDrag(fromDate: string): void {
    this.dragSourceDate.set(fromDate);
    this.dragTargetDate.set(undefined);
  }

  protected allowDrop(event: DragEvent, date: string): void {
    const validTarget =
      this.canUpdateTasks() &&
      this.dragSourceDate() !== date &&
      Array.from(event.dataTransfer?.types ?? []).includes(
        calendarTaskDragType
      );

    if (!validTarget) {
      this.dragTargetDate.set(undefined);
      return;
    }

    event.preventDefault();
    this.dragTargetDate.set(date);
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  protected leaveGrid(event: DragEvent): void {
    const grid = event.currentTarget as HTMLElement;
    const nextTarget = event.relatedTarget;
    if (nextTarget instanceof Node && grid.contains(nextTarget)) {
      return;
    }

    this.dragTargetDate.set(undefined);
  }

  protected endTaskDrag(): void {
    this.dragSourceDate.set(undefined);
    this.dragTargetDate.set(undefined);
  }

  protected dropTask(event: DragEvent, toDate: string): void {
    this.endTaskDrag();

    if (!this.canUpdateTasks() || !event.dataTransfer) {
      return;
    }

    const payload = parseDragPayload(
      event.dataTransfer.getData(calendarTaskDragType)
    );
    const task = this.tasks().find((item) => item.id === payload?.taskId);

    if (!task || !payload || payload.fromDate === toDate) {
      return;
    }

    event.preventDefault();
    this.taskMoved.emit({ task, fromDate: payload.fromDate, toDate });
  }
}

const parseDragPayload = (
  value: string
): { taskId: number; fromDate: string } | undefined => {
  try {
    const payload = JSON.parse(value) as {
      taskId?: unknown;
      fromDate?: unknown;
    };
    return typeof payload.taskId === 'number' &&
      typeof payload.fromDate === 'string'
      ? { taskId: payload.taskId, fromDate: payload.fromDate }
      : undefined;
  } catch {
    return undefined;
  }
};
