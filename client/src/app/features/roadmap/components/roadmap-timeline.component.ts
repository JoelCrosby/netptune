import { Component, computed, input, output, signal } from '@angular/core';
import { RelationCategory } from '@core/models/relation-type';
import { LucideChevronDown, LucideChevronRight } from '@lucide/angular';
import {
  addDays,
  clippedRangeLeft,
  clippedRangeWidth,
  dateLabel,
  inclusiveDayCount,
  monthLabel,
  timelineDayWidth,
  todayDate,
  yearLabel,
} from '@static/components/timeline/timeline-date-geometry';
import { TimelineDependenciesComponent } from '@static/components/timeline/timeline-dependencies.component';
import { TimelineHeaderComponent } from '@static/components/timeline/timeline-header.component';
import { TimelineLaneComponent } from '@static/components/timeline/timeline-lane.component';
import {
  TimelineDependency,
  TimelineHeaderGroup,
  TimelineRange,
  TimelineTick,
  TimelineZoom,
} from '@static/components/timeline/timeline.models';
import { timelineHeaderHeight } from '@static/components/timeline/timeline-range-layout';
import {
  RoadmapProjectGroup,
  RoadmapScheduleChange,
  RoadmapTask,
  RoadmapViewModel,
  roadmapTaskDragType,
} from '../models/roadmap.models';
import {
  buildRoadmapGroups,
  filterCollapsedRoadmapGroups,
} from '../utils/roadmap-row-builder';
import { countOffscreenDependencies } from '../utils/roadmap-relation-index';
import { RoadmapTaskRowComponent } from './roadmap-task-row.component';

const defaultTaskColumnWidth = 320;
const projectRowHeight = 36;
const taskRowHeight = 44;

@Component({
  selector: 'app-roadmap-timeline',
  imports: [
    LucideChevronDown,
    LucideChevronRight,
    TimelineDependenciesComponent,
    TimelineHeaderComponent,
    TimelineLaneComponent,
    RoadmapTaskRowComponent,
  ],
  host: { class: 'block min-h-0 flex-1' },
  template: `
    <div
      class="custom-scroll bg-card h-full overflow-auto"
      [class.ring-2]="taskDragActive()"
      [class.ring-primary]="taskDragActive()"
      role="region"
      aria-label="Task timeline"
      (dragenter)="showTaskDropTarget($event)"
      (dragover)="allowTaskDrop($event)"
      (dragleave)="hideTaskDropTarget($event)"
      (drop)="scheduleDroppedTask($event)">
      <div [style.width.px]="totalWidth()" class="relative min-h-full">
        <app-timeline-header
          itemLabel="Task"
          [itemColumnWidth]="taskColumnWidth()"
          [itemColumnResizable]="true"
          [canvasWidth]="canvasWidth()"
          [dayWidth]="dayWidth()"
          [majorIntervalDays]="majorIntervalDays()"
          [highlightDate]="today"
          [from]="from()"
          [to]="to()"
          [ticks]="ticks()"
          [headerGroups]="headerGroups()"
          [ranges]="sprintRanges()"
          (itemColumnWidthChanged)="taskColumnWidth.set($event)" />

        @for (group of groups(); track group.id) {
          <div class="border-border bg-muted/30 flex h-9 border-b">
            <button
              type="button"
              class="border-border bg-muted/5 hover:bg-muted/60 sticky left-0 z-16 flex shrink-0 cursor-pointer items-center gap-2 border-r px-3 text-left text-sm font-semibold"
              [style.width.px]="taskColumnWidth()"
              [attr.aria-expanded]="!isProjectCollapsed(group.id)"
              [attr.aria-label]="projectCollapseLabel(group)"
              (click)="toggleProject(group.id)">
              @if (isProjectCollapsed(group.id)) {
                <svg lucideChevronRight class="h-4 w-4"></svg>
              } @else {
                <svg lucideChevronDown class="h-4 w-4"></svg>
              }
              <span class="truncate">{{ group.name }}</span>
            </button>
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
              [collapsed]="isTaskCollapsed(row.task.id)"
              [editable]="canUpdateTasks()"
              [busy]="pendingTaskIds().has(row.task.id)"
              [taskColumnWidth]="taskColumnWidth()"
              [canvasWidth]="canvasWidth()"
              [dayWidth]="dayWidth()"
              [majorIntervalDays]="majorIntervalDays()"
              [highlightDate]="today"
              [from]="from()"
              [to]="to()"
              (collapseToggled)="toggleTask($event)"
              (scheduleChanged)="scheduleChanged.emit($event)"
              (taskSelected)="taskSelected.emit($event)" />
          }
        } @empty {
          <div class="text-muted-foreground p-12 text-center text-sm">
            No scheduled tasks overlap this range.
          </div>
        }

        <div
          class="pointer-events-none absolute"
          [style.left.px]="taskColumnWidth()"
          [style.top.px]="headerHeight()">
          <app-timeline-dependencies
            [dependencies]="dependencies()"
            [width]="canvasWidth()"
            [height]="bodyHeight()" />
        </div>
      </div>
    </div>
  `,
})
export class RoadmapTimelineComponent {
  readonly view = input.required<RoadmapViewModel>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly zoom = input.required<TimelineZoom>();
  readonly canUpdateTasks = input(false);
  readonly pendingTaskIds = input<ReadonlySet<number>>(new Set());
  readonly taskSelected = output<RoadmapTask>();
  readonly scheduleChanged = output<RoadmapScheduleChange>();
  readonly collapsedProjectIds = signal<ReadonlySet<number>>(new Set());
  readonly collapsedTaskIds = signal<ReadonlySet<number>>(new Set());
  readonly taskDragActive = signal(false);

  readonly taskColumnWidth = signal(defaultTaskColumnWidth);
  readonly today = todayDate();
  readonly dayWidth = computed(() => timelineDayWidth(this.zoom()));
  readonly majorIntervalDays = computed(() =>
    this.zoom() === 'day' ? 1 : this.zoom() === 'week' ? 7 : 30
  );

  readonly canvasWidth = computed(
    () =>
      Math.max(1, inclusiveDayCount(this.from(), this.to())) * this.dayWidth()
  );

  readonly totalWidth = computed(
    () => this.taskColumnWidth() + this.canvasWidth()
  );

  readonly allGroups = computed(() =>
    buildRoadmapGroups(this.view().tasks, this.view().relations)
  );

  readonly groups = computed(() => {
    const groups = filterCollapsedRoadmapGroups(
      this.allGroups(),
      this.collapsedProjectIds(),
      this.collapsedTaskIds()
    );
    const visibleTaskIds = new Set(
      groups.flatMap((group) => group.tasks.map((row) => row.task.id))
    );

    const offscreenBlockerCounts = countOffscreenDependencies(
      this.view().relations,
      visibleTaskIds,
      RelationCategory.dependency
    );

    return groups.map((group) => ({
      ...group,
      tasks: group.tasks.map((row) => ({
        ...row,
        offscreenBlockedByCount: offscreenBlockerCounts.get(row.task.id) ?? 0,
      })),
    }));
  });

  readonly sprintRanges = computed<TimelineRange[]>(() =>
    this.view().sprints.map((sprint) => ({
      id: sprint.id,
      label: sprint.name,
      startDate: sprint.startDate,
      endDate: sprint.endDate,
    }))
  );
  readonly headerHeight = computed(() =>
    timelineHeaderHeight(this.sprintRanges())
  );

  readonly ticks = computed<TimelineTick[]>(() => {
    const count = Math.max(0, inclusiveDayCount(this.from(), this.to()));
    const ticks: TimelineTick[] = [];

    for (let offset = 0; offset < count; offset += 1) {
      const date = addDays(this.from(), offset);

      if (!this.showTick(date, offset)) {
        continue;
      }

      ticks.push({
        date,
        label: dateLabel(date, this.zoom()),
        left: offset * this.dayWidth() + 4,
      });
    }

    return ticks;
  });
  readonly headerGroups = computed<TimelineHeaderGroup[]>(() => {
    const count = Math.max(0, inclusiveDayCount(this.from(), this.to()));
    const groups: TimelineHeaderGroup[] = [];

    for (let offset = 0; offset < count; offset += 1) {
      const date = addDays(this.from(), offset);
      const id = this.zoom() === 'month' ? date.slice(0, 4) : date.slice(0, 7);
      const previous = groups.at(-1);

      if (previous?.id === id) {
        previous.width += this.dayWidth();
        continue;
      }

      groups.push({
        id,
        label: this.zoom() === 'month' ? yearLabel(date) : monthLabel(date),
        left: offset * this.dayWidth(),
        width: this.dayWidth(),
      });
    }

    return groups;
  });

  readonly bodyHeight = computed(
    () =>
      this.groups().length * projectRowHeight +
      this.groups().reduce(
        (total, group) => total + group.tasks.length * taskRowHeight,
        0
      )
  );

  readonly dependencies = computed<TimelineDependency[]>(() => {
    const taskPositions = this.taskPositions();

    return this.view()
      .relations.filter(
        (relation) => relation.category === RelationCategory.dependency
      )
      .flatMap((relation) => {
        const source = taskPositions.get(relation.sourceTaskId);
        const target = taskPositions.get(relation.targetTaskId);

        if (!source || !target) {
          return [];
        }

        return [
          {
            id: relation.id,
            sourceX: source.right,
            sourceY: source.y,
            targetX: target.left,
            targetY: target.y,
          },
        ];
      });
  });

  toggleProject(projectId: number): void {
    this.collapsedProjectIds.update((ids) => toggledSet(ids, projectId));
  }

  toggleTask(taskId: number): void {
    this.collapsedTaskIds.update((ids) => toggledSet(ids, taskId));
  }

  isProjectCollapsed(projectId: number): boolean {
    return this.collapsedProjectIds().has(projectId);
  }

  isTaskCollapsed(taskId: number): boolean {
    return this.collapsedTaskIds().has(taskId);
  }

  projectCollapseLabel(group: RoadmapProjectGroup): string {
    return `${this.isProjectCollapsed(group.id) ? 'Expand' : 'Collapse'} ${group.name}`;
  }

  showTaskDropTarget(event: DragEvent): void {
    if (this.isRoadmapTaskDrag(event)) {
      this.taskDragActive.set(true);
    }
  }

  allowTaskDrop(event: DragEvent): void {
    if (!this.isRoadmapTaskDrag(event) || !this.canUpdateTasks()) {
      return;
    }

    event.preventDefault();
    this.taskDragActive.set(true);

    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  hideTaskDropTarget(event: DragEvent): void {
    const target = event.currentTarget as HTMLElement;
    const nextTarget = event.relatedTarget as Node | null;

    if (!nextTarget || !target.contains(nextTarget)) {
      this.taskDragActive.set(false);
    }
  }

  scheduleDroppedTask(event: DragEvent): void {
    this.taskDragActive.set(false);

    if (!this.canUpdateTasks() || !event.dataTransfer) {
      return;
    }

    const task = parseDraggedTask(
      event.dataTransfer.getData(roadmapTaskDragType)
    );

    if (!task) {
      return;
    }

    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    const timelineX =
      event.clientX -
      target.getBoundingClientRect().left +
      target.scrollLeft -
      this.taskColumnWidth();
    const lastDayOffset = Math.max(
      0,
      inclusiveDayCount(this.from(), this.to()) - 1
    );
    const dayOffset = Math.min(
      lastDayOffset,
      Math.max(0, Math.floor(timelineX / this.dayWidth()))
    );
    const date = addDays(this.from(), dayOffset);

    this.scheduleChanged.emit({
      task,
      schedule: { startDate: date, endDate: date },
    });
  }

  private showTick(date: string, offset: number): boolean {
    if (this.zoom() === 'day') {
      return true;
    }

    if (this.zoom() === 'week') {
      return offset % 7 === 0;
    }

    return offset === 0 || date.endsWith('-01');
  }

  private isRoadmapTaskDrag(event: DragEvent): boolean {
    return (
      this.canUpdateTasks() &&
      Array.from(event.dataTransfer?.types ?? []).includes(roadmapTaskDragType)
    );
  }

  private taskPositions(): Map<
    number,
    { left: number; right: number; y: number }
  > {
    const positions = new Map<
      number,
      { left: number; right: number; y: number }
    >();
    let top = 0;

    for (const group of this.groups()) {
      top += projectRowHeight;

      for (const row of group.tasks) {
        const start = row.task.startDate ?? row.task.dueDate ?? this.from();
        const end = row.task.dueDate ?? row.task.startDate ?? start;
        const left = clippedRangeLeft(this.from(), start, this.dayWidth());
        const width = clippedRangeWidth(
          this.from(),
          this.to(),
          start,
          end,
          this.dayWidth()
        );
        positions.set(row.task.id, {
          left,
          right: Math.min(this.canvasWidth(), left + width),
          y: top + taskRowHeight / 2,
        });
        top += taskRowHeight;
      }
    }

    return positions;
  }
}

const toggledSet = (
  values: ReadonlySet<number>,
  value: number
): ReadonlySet<number> => {
  const updated = new Set(values);

  if (updated.has(value)) {
    updated.delete(value);
  } else {
    updated.add(value);
  }

  return updated;
};

const parseDraggedTask = (value: string): RoadmapTask | undefined => {
  try {
    const task = JSON.parse(value) as Partial<RoadmapTask>;
    return typeof task.id === 'number' && typeof task.systemId === 'string'
      ? (task as RoadmapTask)
      : undefined;
  } catch {
    return undefined;
  }
};
