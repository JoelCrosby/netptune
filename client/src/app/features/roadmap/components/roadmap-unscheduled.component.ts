import {
  Component,
  computed,
  input,
  output,
  Signal,
  signal,
  viewChild,
} from '@angular/core';
import { Params } from '@angular/router';
import { taskColumns } from '@core/tasks/task-columns';
import { TaskViewFiltersComponent } from '@shared/components/task-view-filters/task-view-filters.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { scrollHeights } from '@static/components/datatable/datatable-classes';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { TaskTableComponent } from '@static/components/task-table.component';
import {
  defaultUnscheduledFilters,
  RoadmapScheduleChange,
  RoadmapTask,
  roadmapTaskDragType,
  RoadmapUnscheduledFilters,
} from '../models/roadmap.models';

@Component({
  selector: 'app-roadmap-unscheduled',
  imports: [
    CheckboxComponent,
    DatatableCellTemplateDirective,
    TaskTableComponent,
    TaskViewFiltersComponent,
  ],
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

      <app-task-view-filters
        i18n-aria-label="Accessible name of the unscheduled task filter row"
        aria-label="Filter unscheduled tasks"
        role="group"
        [search]="filters().search ?? undefined"
        [assigneeIds]="filters().assigneeIds"
        [tagNames]="filters().tagNames"
        [statusIds]="filters().statusIds"
        [supportsFlags]="true"
        [flagged]="filters().flagged"
        [supportsUntagged]="true"
        [untagged]="filters().untagged"
        [extraFiltersActive]="filters().includeCompleted"
        (searchChanged)="onFiltersChanged({ search: $event })"
        (assigneeIdsChanged)="onFiltersChanged({ assigneeIds: $event })"
        (tagNamesChanged)="onTagNamesChanged($event)"
        (statusIdsChanged)="onFiltersChanged({ statusIds: $event })"
        (flaggedChanged)="onFiltersChanged({ flagged: $event })"
        (untaggedChanged)="onUntaggedChanged($event)"
        (cleared)="filters.set(defaultFilters)">
        <div class="border-border border-l pl-3">
          <app-checkbox
            class="text-sm"
            [checked]="filters().includeCompleted"
            (changed)="onFiltersChanged({ includeCompleted: $event })">
            <span i18n="Toggle that reveals tasks in a done or inactive status">
              Show completed
            </span>
          </app-checkbox>
        </div>
      </app-task-view-filters>

      <app-task-table
        key="roadmap-unscheduled-tasks"
        url="api/roadmap/unscheduled-tasks"
        tableClass="md:min-w-[820px] table-fixed"
        i18n-emptyMessage="Empty state for the unscheduled task list"
        emptyMessage="No unscheduled tasks match the current filters."
        i18n-itemLabel="Plural noun for tasks, used in the selection summary"
        itemLabel="tasks"
        [containerClass]="scrollHeights.panel"
        [columns]="columns()"
        [params]="params"
        [reloadSignal]="reloadSignal()"
        [stickyHeader]="true">
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
  readonly canUpdateTasks = input(false);
  readonly scheduleDate = input.required<string>();
  readonly reloadSignal = input.required<Signal<unknown>>();
  readonly taskSelected = output<RoadmapTask>();
  readonly scheduleRequested = output<RoadmapScheduleChange>();
  private readonly table = viewChild(TaskTableComponent<RoadmapTask>);
  readonly totalCount = computed(() => this.table()?.loadedCount() ?? 0);

  readonly scrollHeights = scrollHeights;

  readonly filters = signal<RoadmapUnscheduledFilters>(
    defaultUnscheduledFilters
  );

  protected readonly defaultFilters = defaultUnscheduledFilters;

  readonly params = computed<Params>(() => {
    const projectId = this.projectId();
    const sprintId = this.sprintId();
    const filters = this.filters();
    const search = filters.search?.trim();

    return {
      ...(projectId ? { projectIds: projectId } : {}),
      ...(sprintId ? { sprintIds: sprintId } : {}),
      ...(search ? { search } : {}),
      ...(filters.assigneeIds.length ? { assignees: filters.assigneeIds } : {}),
      ...(filters.tagNames.length ? { tags: filters.tagNames } : {}),
      ...(filters.untagged ? { hasTags: false } : {}),
      ...(filters.statusIds.length ? { statusIds: filters.statusIds } : {}),
      ...(filters.flagged ? { hasFlags: true } : {}),
      ...(filters.includeCompleted ? { includeCompleted: true } : {}),
    };
  });

  private readonly scheduleColumn: DatatableColumn<RoadmapTask> = {
    id: 'schedule',
    header: $localize`:Column heading for the schedule action:Schedule`,
    visibleOnMobile: true,
    widthClass: 'w-36',
  };

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

  onFiltersChanged(changes: Partial<RoadmapUnscheduledFilters>): void {
    this.filters.update((filters) => ({ ...filters, ...changes }));
  }

  onTagNamesChanged(tagNames: string[]): void {
    this.onFiltersChanged({ tagNames, untagged: false });
  }

  onUntaggedChanged(untagged: boolean): void {
    this.onFiltersChanged(untagged ? { untagged, tagNames: [] } : { untagged });
  }

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
