import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  linkedSignal,
  model,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ScheduledTask } from '@core/models/scheduled-task';
import { DialogService } from '@core/services/dialog.service';
import { TaskSchedulingService } from '@core/services/task-scheduling.service';
import { SseService } from '@core/sse/sse.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { debounceTime, Subject } from 'rxjs';
import {
  CalendarDay,
  CalendarTaskMove,
  CalendarTaskMoveRequest,
  CalendarViewModel,
} from '../../models/calendar.models';
import {
  CalendarMoveTaskDialogComponent,
  CalendarMoveTaskDialogResult,
} from '../../dialogs/calendar-move-task-dialog.component';
import { moveTaskSchedule } from '../../utils/calendar-tasks';
import { CalendarMonthGridComponent } from '../calendar-month-grid/calendar-month-grid.component';
import { CalendarSelectedDayTableComponent } from '../calendar-selected-day-table/calendar-selected-day-table.component';

@Component({
  selector: 'app-calendar-planning-month',
  imports: [CalendarMonthGridComponent, CalendarSelectedDayTableComponent],
  host: { class: 'contents' },
  template: `
    <div class="custom-scroll min-h-0 flex-1 overflow-auto">
      <app-calendar-month-grid
        [days]="days()"
        [tasks]="tasks()"
        [selectedDate]="selectedDate()"
        [canUpdateTasks]="canUpdateTasks()"
        [pendingTaskIds]="pendingTaskIds()"
        (daySelected)="selectedDate.set($event)"
        (taskSelected)="taskSelected.emit($event)"
        (taskMoveRequested)="requestTaskMove($event)"
        (taskMoved)="moveTask($event)" />
    </div>

    <app-calendar-selected-day-table
      [date]="selectedDate()"
      [projectId]="projectId()"
      [sprintId]="sprintId()"
      [reloadSignal]="dayTableReload"
      (taskSelected)="taskSelected.emit($event)" />

    <span class="sr-only" aria-live="polite">{{ announcement() }}</span>
  `,
  styles: ``,
})
export class CalendarPlanningMonthComponent {
  readonly view = input.required<CalendarViewModel>();
  readonly days = input.required<CalendarDay[]>();
  readonly selectedDate = model.required<string>();
  readonly realtimeGroup = input<string>();
  readonly canUpdateTasks = input(false);
  readonly projectId = input<number>();
  readonly sprintId = input<number>();
  readonly taskSelected = output<ScheduledTask>();
  readonly refreshRequested = output();

  private readonly scheduling = inject(TaskSchedulingService);
  private readonly dialog = inject(DialogService);
  private readonly snackbar = inject(SnackbarService);
  private readonly sse = inject(SseService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshSignals = new Subject<void>();
  private refreshQueued = false;

  readonly tasks = linkedSignal(() => this.view().tasks);
  readonly pendingTaskIds = signal<ReadonlySet<number>>(new Set());
  readonly announcement = signal('');
  readonly dayTableReload = this.view;

  constructor() {
    this.refreshSignals
      .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.flushRefresh());

    effect((onCleanup) => {
      const group = this.realtimeGroup();
      if (!group) {
        return;
      }

      this.sse.connect(group, () => this.requestRefresh());
      onCleanup(() => this.sse.disconnect());
    });
  }

  requestRefresh(): void {
    this.refreshSignals.next();
  }

  protected requestTaskMove(request: CalendarTaskMoveRequest): void {
    if (!this.canUpdateTasks() || this.pendingTaskIds().has(request.task.id)) {
      return;
    }

    const dialogRef = this.dialog.open<
      CalendarMoveTaskDialogResult,
      CalendarTaskMoveRequest,
      CalendarMoveTaskDialogComponent
    >(CalendarMoveTaskDialogComponent, {
      ariaLabel: `Move ${request.task.systemId}`,
      autoFocus: 'first-tabbable',
      data: request,
      maxWidth: 'calc(100vw - 2rem)',
      width: '28rem',
    });

    dialogRef.closed
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result || result.date === request.fromDate) {
          return;
        }

        this.moveTask({
          task: request.task,
          fromDate: request.fromDate,
          toDate: result.date,
        });
      });
  }

  protected moveTask(change: CalendarTaskMove): void {
    if (!this.canUpdateTasks() || this.pendingTaskIds().has(change.task.id)) {
      return;
    }

    const schedule = moveTaskSchedule(
      change.task,
      change.fromDate,
      change.toDate
    );
    const previous = {
      startDate: change.task.startDate ?? null,
      dueDate: change.task.dueDate ?? null,
    };

    this.updateOptimisticTask(
      change.task.id,
      schedule.startDate,
      schedule.endDate
    );
    this.setPending(change.task.id, true);
    this.scheduling
      .updateSchedule(change.task.id, schedule)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.setPending(change.task.id, false);
          this.announcement.set(
            `${change.task.systemId} moved to ${change.toDate}`
          );
          this.snackbar.success('Task schedule updated');
          this.requestRefresh();
        },
        error: () => {
          this.updateOptimisticTask(
            change.task.id,
            previous.startDate,
            previous.dueDate
          );
          this.setPending(change.task.id, false);
          this.announcement.set(`${change.task.systemId} could not be moved`);
          this.snackbar.error('Task schedule could not be updated');
          if (this.refreshQueued) {
            this.requestRefresh();
          }
        },
      });
  }

  private flushRefresh(): void {
    if (this.pendingTaskIds().size > 0) {
      this.refreshQueued = true;
      return;
    }

    this.refreshQueued = false;
    this.refreshRequested.emit();
  }

  private updateOptimisticTask(
    taskId: number,
    startDate: string | null,
    dueDate: string | null
  ): void {
    this.tasks.update((tasks) =>
      tasks.map((task) =>
        task.id === taskId ? { ...task, startDate, dueDate } : task
      )
    );
  }

  private setPending(taskId: number, pending: boolean): void {
    this.pendingTaskIds.update((ids) => {
      const updated = new Set(ids);
      if (pending) {
        updated.add(taskId);
      } else {
        updated.delete(taskId);
      }
      return updated;
    });
  }
}
