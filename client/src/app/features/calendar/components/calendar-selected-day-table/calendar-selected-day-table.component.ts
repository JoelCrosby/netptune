import { Component, Signal, computed, input, output } from '@angular/core';
import { Params } from '@angular/router';
import { ScheduledTask } from '@core/models/scheduled-task';
import { colorSwatchClass } from '@core/util/colors/colors';
import { AvatarStackComponent } from '@static/components/avatar-stack/avatar-stack.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { calendarDayLabel } from '../../utils/calendar-range';
import { taskEndsOn, taskStartsOn } from '../../utils/calendar-tasks';

@Component({
  selector: 'app-calendar-selected-day-table',
  imports: [
    AvatarStackComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
  ],
  template: `
    <section
      class="border-border bg-card border-t"
      aria-labelledby="selected-day-heading">
      <h2
        id="selected-day-heading"
        class="text-primary px-3 py-3 text-sm font-semibold">
        {{ dateLabel() }}
      </h2>

      <app-datatable
        containerClass="max-h-44 overflow-auto border-0 rounded-none"
        tableClass="text-xs"
        emptyCellClass="py-5"
        emptyMessage="No scheduled tasks for this day."
        itemLabel="tasks"
        [stickyHeader]="true"
        [data]="data()">
        <ng-template appDatatableCell="name" let-task>
          <button
            type="button"
            class="hover:text-primary focus-visible:ring-ring max-w-96 truncate rounded text-left font-medium focus-visible:ring-2 focus-visible:outline-none"
            [attr.aria-label]="openTaskLabel(task)"
            (click)="taskSelected.emit(task)">
            {{ task.name }}
          </button>
        </ng-template>

        <ng-template appDatatableCell="systemId" let-task>
          <span class="text-muted-foreground font-mono">{{
            task.systemId
          }}</span>
        </ng-template>

        <ng-template appDatatableCell="statusName" let-task>
          <span class="inline-flex items-center gap-1.5">
            <span
              [class]="
                'h-2 w-2 rounded-full ' + colorSwatchClass(task.statusColor)
              "></span>
            <span>{{ task.statusName }}</span>
          </span>
        </ng-template>

        <ng-template appDatatableCell="assignees" let-task>
          @if (task.assignees.length > 0) {
            <app-avatar-stack [avatars]="task.assignees" />
          } @else {
            <span class="text-muted-foreground">Unassigned</span>
          }
        </ng-template>
      </app-datatable>
    </section>
  `,
  styles: ``,
})
export class CalendarSelectedDayTableComponent {
  readonly colorSwatchClass = colorSwatchClass;
  readonly date = input.required<string>();
  readonly projectId = input<number>();
  readonly sprintId = input<number>();
  readonly search = input<string>();
  readonly assigneeIds = input<string[]>([]);
  readonly tagNames = input<string[]>([]);
  readonly statusIds = input<number[]>([]);
  readonly reloadSignal = input.required<Signal<unknown>>();
  readonly taskSelected = output<ScheduledTask>();

  readonly dateLabel = computed(() => calendarDayLabel(this.date()));
  private readonly params = computed<Params>(() => ({
    date: this.date(),
    projectId: this.projectId(),
    sprintId: this.sprintId(),
    search: this.search(),
    assignees: this.assigneeIds(),
    tags: this.tagNames(),
    statusIds: this.statusIds(),
  }));
  readonly data = computed<DatatableDataSource<ScheduledTask>>(() => ({
    key: 'calendar-selected-day-tasks',
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
        cellClass: 'min-w-48',
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
        id: 'schedule',
        header: 'Schedule',
        accessor: (task) => scheduleLabel(task),
        sortable: true,
        widthClass: 'w-52',
      },
      {
        id: 'assignees',
        header: 'Assignees',
        accessor: (task) =>
          task.assignees.map((assignee) => assignee.displayName).join(', '),
        sortable: true,
        widthClass: 'w-40',
      },
    ],
    resource: {
      url: 'api/calendar/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_index, task) => task.id,
    reloadSignal: this.reloadSignal(),
  }));

  protected openTaskLabel(task: ScheduledTask): string {
    return `Open ${task.systemId}, ${task.name}`;
  }
}

const scheduleLabel = (task: ScheduledTask): string => {
  const start = taskStartsOn(task);
  const end = taskEndsOn(task);
  return start === end ? (start ?? 'Unscheduled') : `${start} – ${end}`;
};
