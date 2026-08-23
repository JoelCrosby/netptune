import {
  Component,
  Signal,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { Params } from '@angular/router';
import { taskColumns } from '@core/tasks/task-columns';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { scrollHeights } from '@static/components/datatable/datatable-classes';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { TaskTableComponent } from '@static/components/task-table.component';
import {
  RoadmapScheduleChange,
  RoadmapTask,
  roadmapTaskDragType,
} from '../models/roadmap.models';

@Component({
  selector: 'app-roadmap-unscheduled',
  imports: [DatatableCellTemplateDirective, TaskTableComponent],
  host: { class: 'block' },
  template: `
    <section class="mt-4 flex flex-col gap-3">
      <h2 class="font-semibold">
        <span
          i18n="Heading above tasks without dates. COUNT is how many there are">
          Unscheduled tasks ({{
            totalCount()  // i18n(ph="COUNT")
          }})
        </span>
      </h2>

      <app-task-table
        key="roadmap-unscheduled-tasks"
        url="api/roadmap/unscheduled-tasks"
        tableClass="min-w-[820px] table-fixed"
        i18n-emptyMessage="Empty state for the unscheduled task list"
        emptyMessage="No unscheduled tasks match the current filters."
        i18n-itemLabel="Plural noun for tasks, used in the selection summary"
        itemLabel="tasks"
        [containerClass]="scrollHeights.panel"
        [columns]="columns()"
        [params]="params"
        [reloadSignal]="reloadSignal()"
        [stickyHeader]="true"
        (loaded)="totalCount.set($event.totalCount)">
        <ng-template appDatatableCell="name" let-task>
          <button
            type="button"
            class="block w-full cursor-pointer truncate text-left font-medium hover:underline"
            [class.cursor-grab]="canUpdateTasks()"
            [attr.draggable]="canUpdateTasks()"
            [title]="taskDragTitle(task)"
            (dragstart)="startTaskDrag($event, task)"
            (click)="taskSelected.emit(task)">
            {{ task.name }}
          </button>
        </ng-template>

        <ng-template appDatatableCell="schedule" let-task>
          <button
            type="button"
            class="hover:bg-muted rounded border px-2 py-1 text-xs"
            [attr.aria-label]="scheduleLabel(task)"
            [title]="scheduleLabel(task)"
            (click)="scheduleAtRangeStart(task)">
            <span i18n="Button that gives an unscheduled task dates">
              Schedule
            </span>
          </button>
        </ng-template>
      </app-task-table>
    </section>
  `,
})
export class RoadmapUnscheduledComponent {
  readonly projectId = input<number>();
  readonly sprintId = input<number>();
  readonly search = input<string>();
  readonly assigneeIds = input<string[]>([]);
  readonly tagNames = input<string[]>([]);
  readonly statusIds = input<number[]>([]);
  readonly canUpdateTasks = input(false);
  readonly scheduleDate = input.required<string>();
  readonly reloadSignal = input.required<Signal<unknown>>();
  readonly taskSelected = output<RoadmapTask>();
  readonly scheduleRequested = output<RoadmapScheduleChange>();
  readonly totalCount = signal(0);

  readonly scrollHeights = scrollHeights;

  readonly params = computed<Params>(() => {
    const projectId = this.projectId();
    const sprintId = this.sprintId();

    return {
      ...(projectId ? { projectIds: projectId } : {}),
      ...(sprintId ? { sprintIds: sprintId } : {}),
      ...(this.search() ? { search: this.search() } : {}),
      ...(this.assigneeIds().length ? { assignees: this.assigneeIds() } : {}),
      ...(this.tagNames().length ? { tags: this.tagNames() } : {}),
      ...(this.statusIds().length ? { statusIds: this.statusIds() } : {}),
    };
  });

  private readonly scheduleColumn: DatatableColumn<RoadmapTask> = {
    id: 'schedule',
    header: $localize`:Column heading for the schedule action:Schedule`,
    widthClass: 'w-36',
  };

  // The roadmap endpoint names its status sort field statusName rather than
  // status, so the catalog column carries an endpoint-specific sort key here.
  private readonly baseColumns = taskColumns<RoadmapTask>(
    ['systemId', 'name', 'project', 'status', 'priority', 'assignees'],
    {
      overrides: {
        name: { cellClass: 'min-w-0' },
        status: { sortKey: 'statusName' },
      },
    }
  );

  readonly columns = computed<DatatableColumn<RoadmapTask>[]>(() => {
    return this.canUpdateTasks()
      ? [...this.baseColumns, this.scheduleColumn]
      : this.baseColumns;
  });

  startTaskDrag(event: DragEvent, task: RoadmapTask): void {
    if (!this.canUpdateTasks() || !event.dataTransfer) {
      event.preventDefault();
      return;
    }

    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData(roadmapTaskDragType, JSON.stringify(task));
  }

  scheduleAtRangeStart(task: RoadmapTask): void {
    const date = this.scheduleDate();
    this.scheduleRequested.emit({
      task,
      schedule: { startDate: date, endDate: date },
    });
  }

  scheduleLabel(task: RoadmapTask): string {
    return `Schedule ${task.systemId} on ${this.scheduleDate()}`;
  }

  taskDragTitle(task: RoadmapTask): string {
    return this.canUpdateTasks()
      ? `Open ${task.systemId}, or drag it onto the timeline to schedule it`
      : `Open ${task.systemId}`;
  }
}
