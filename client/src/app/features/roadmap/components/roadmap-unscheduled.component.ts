import {
  Component,
  Signal,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { Params } from '@angular/router';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { AvatarStackComponent } from '@static/components/avatar-stack/avatar-stack.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import {
  RoadmapScheduleChange,
  RoadmapTask,
  roadmapTaskDragType,
} from '../models/roadmap.models';

@Component({
  selector: 'app-roadmap-unscheduled',
  imports: [
    AvatarStackComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    TaskScopeIdComponent,
  ],
  host: { class: 'block' },
  template: `
    <section class="mt-4 flex flex-col gap-3">
      <h2 class="font-semibold">Unscheduled tasks ({{ totalCount() }})</h2>

      <app-datatable
        containerClass="max-h-[32rem] overflow-auto"
        tableClass="min-w-[820px] table-fixed"
        rowClass="bg-card"
        emptyMessage="No unscheduled tasks match the current filters."
        itemLabel="tasks"
        [data]="data()"
        [stickyHeader]="true"
        (loaded)="totalCount.set($event.totalCount)">
        <ng-template appDatatableCell="systemId" let-task>
          <app-task-scope-id [id]="task.systemId" />
        </ng-template>

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
            Schedule
          </button>
        </ng-template>

        <ng-template appDatatableCell="statusName" let-task>
          <span class="inline-flex min-w-0 items-center gap-2">
            <span
              class="h-2 w-2 shrink-0 rounded-full"
              [style.background-color]="task.statusColor"></span>
            <span class="truncate">{{ task.statusName }}</span>
          </span>
        </ng-template>

        <ng-template appDatatableCell="priority" let-task>
          @if (task.priority !== null && task.priority !== undefined) {
            <span
              class="text-sm font-medium"
              [class]="priorityColor(task.priority)">
              {{ priorityLabel(task.priority) }}
            </span>
          } @else {
            <span class="text-muted text-sm">None</span>
          }
        </ng-template>

        <ng-template appDatatableCell="assignees" let-task>
          @if (task.assignees.length > 0) {
            <app-avatar-stack [avatars]="task.assignees" />
          } @else {
            <span class="text-muted text-sm">Unassigned</span>
          }
        </ng-template>
      </app-datatable>
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
  readonly totalCount = signal(0);

  private readonly params = computed<Params>(() => {
    const projectId = this.projectId();
    const sprintId = this.sprintId();

    return {
      ...(projectId ? { projectIds: projectId } : {}),
      ...(sprintId ? { sprintIds: sprintId } : {}),
    };
  });

  readonly data = computed<DatatableDataSource<RoadmapTask>>(() => ({
    key: 'roadmap-unscheduled-tasks',
    columns: [
      {
        id: 'systemId',
        header: 'Key',
        accessor: 'systemId',
        sortable: true,
        widthClass: 'w-28',
      },
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-0',
      },
      {
        id: 'projectName',
        header: 'Project',
        accessor: 'projectName',
        sortable: true,
        widthClass: 'w-44',
      },
      {
        id: 'statusName',
        header: 'Status',
        accessor: 'statusName',
        sortable: true,
        widthClass: 'w-40',
      },
      {
        id: 'priority',
        header: 'Priority',
        accessor: 'priority',
        sortable: true,
        widthClass: 'w-32',
      },
      {
        id: 'assignees',
        header: 'Assignees',
        accessor: (task) =>
          task.assignees.map((assignee) => assignee.displayName).join(', '),
        sortable: true,
        widthClass: 'w-40',
      },
      ...(this.canUpdateTasks()
        ? [
            {
              id: 'schedule' as const,
              header: 'Schedule',
              widthClass: 'w-36',
            },
          ]
        : []),
    ],
    resource: {
      url: 'api/roadmap/unscheduled-tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_, task) => task.id,
    reloadSignal: this.reloadSignal(),
  }));

  priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabels[priority];
  }

  priorityColor(priority: TaskPriority): string {
    return taskPriorityColors[priority];
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
