import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SseService } from '@core/sse/sse.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TimelineZoom } from '@static/components/timeline/timeline.models';
import { debounceTime, Subject } from 'rxjs';
import {
  RoadmapScheduleChange,
  RoadmapTask,
  RoadmapViewModel,
} from '../models/roadmap.models';
import { RoadmapPlanningService } from '../services/roadmap-planning.service';
import { RoadmapTimelineComponent } from './roadmap-timeline.component';

@Component({
  selector: 'app-roadmap-planning-timeline',
  imports: [RoadmapTimelineComponent],
  host: { class: 'contents' },
  template: `
    <app-roadmap-timeline
      [view]="optimisticView()"
      [from]="from()"
      [to]="to()"
      [zoom]="zoom()"
      [canUpdateTasks]="canUpdateTasks()"
      [pendingTaskIds]="pendingTaskIds()"
      (scheduleChanged)="updateSchedule($event)"
      (taskSelected)="taskSelected.emit($event)" />
  `,
})
export class RoadmapPlanningTimelineComponent {
  readonly view = input.required<RoadmapViewModel>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly zoom = input.required<TimelineZoom>();
  readonly realtimeGroup = input<string>();
  readonly canUpdateTasks = input(false);
  readonly taskSelected = output<RoadmapTask>();
  readonly refreshRequested = output();

  private readonly planning = inject(RoadmapPlanningService);
  private readonly snackbar = inject(SnackbarService);
  private readonly sse = inject(SseService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshSignals = new Subject<void>();
  private refreshQueued = false;

  readonly optimisticView = linkedSignal(() => this.view());
  readonly pendingTaskIds = signal<ReadonlySet<number>>(new Set());

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

  updateSchedule(change: RoadmapScheduleChange): void {
    const group = this.realtimeGroup();
    const task = this.optimisticView().tasks.find(
      (item) => item.id === change.task.id
    );
    const canStartUpdate =
      !!group &&
      !!task &&
      this.canUpdateTasks() &&
      !this.pendingTaskIds().has(change.task.id);

    if (!canStartUpdate) {
      return;
    }

    const previousSchedule = {
      startDate: task.startDate ?? null,
      endDate: task.dueDate ?? null,
    };
    this.setTaskSchedule(task.id, change.schedule);
    this.setTaskPending(task.id, true);

    this.planning
      .updateSchedule(group, task.id, change.schedule)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.setTaskPending(task.id, false);
          this.snackbar.success('Task schedule updated');
          this.requestRefresh();
        },
        error: () => {
          this.setTaskSchedule(task.id, previousSchedule);
          this.setTaskPending(task.id, false);
          this.snackbar.error('Task schedule could not be updated');

          if (this.refreshQueued) {
            this.requestRefresh();
          }
        },
      });
  }

  requestRefresh(): void {
    this.refreshSignals.next();
  }

  private setTaskSchedule(
    taskId: number,
    schedule: { startDate: string | null; endDate: string | null }
  ): void {
    this.optimisticView.update((view) => ({
      ...view,
      tasks: view.tasks.map((task) =>
        task.id === taskId
          ? {
              ...task,
              startDate: schedule.startDate,
              dueDate: schedule.endDate,
            }
          : task
      ),
    }));
  }

  private setTaskPending(taskId: number, pending: boolean): void {
    this.pendingTaskIds.update((taskIds) => {
      const updated = new Set(taskIds);

      if (pending) {
        updated.add(taskId);
      } else {
        updated.delete(taskId);
      }

      return updated;
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
}
