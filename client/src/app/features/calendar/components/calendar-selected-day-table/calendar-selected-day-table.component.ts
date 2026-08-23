import { Component, Signal, computed, input, output } from '@angular/core';
import { Params } from '@angular/router';
import { ScheduledTask } from '@core/models/scheduled-task';
import { taskColumns } from '@core/tasks/task-columns';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { scrollHeights } from '@static/components/datatable/datatable-classes';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { TaskTableComponent } from '@static/components/task-table.component';
import { calendarDayLabel } from '../../utils/calendar-range';
import { taskEndsOn, taskStartsOn } from '../../utils/calendar-tasks';

@Component({
  selector: 'app-calendar-selected-day-table',
  imports: [DatatableCellTemplateDirective, TaskTableComponent],
  template: `
    <section
      class="border-border bg-card border-t"
      aria-labelledby="selected-day-heading">
      <h2
        id="selected-day-heading"
        class="text-primary px-3 py-3 text-sm font-semibold">
        {{ dateLabel() }}
      </h2>

      <app-task-table
        key="calendar-selected-day-tasks"
        url="api/calendar/tasks"
        tableClass="text-xs"
        emptyCellClass="py-5"
        i18n-emptyMessage="Empty state for the selected calendar day"
        emptyMessage="No scheduled tasks for this day."
        i18n-itemLabel="Plural noun for tasks, used in the selection summary"
        itemLabel="tasks"
        [containerClass]="containerClasses"
        [rounded]="false"
        [columns]="columns"
        [params]="params"
        [reloadSignal]="reloadSignal()"
        [stickyHeader]="true">
        <ng-template appDatatableCell="name" let-task>
          <button
            type="button"
            class="hover:text-primary focus-visible:ring-ring max-w-96 truncate rounded text-left font-medium focus-visible:ring-2 focus-visible:outline-none"
            [attr.aria-label]="openTaskLabel(task)"
            (click)="taskSelected.emit(task)">
            {{ task.name }}
          </button>
        </ng-template>
      </app-task-table>
    </section>
  `,
  styles: ``,
})
export class CalendarSelectedDayTableComponent {
  readonly containerClasses = `${scrollHeights.compact} border-0`;
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
  readonly params = computed<Params>(() => ({
    date: this.date(),
    projectId: this.projectId(),
    sprintId: this.sprintId(),
    search: this.search(),
    assignees: this.assigneeIds(),
    tags: this.tagNames(),
    statusIds: this.statusIds(),
  }));
  private readonly scheduleColumn: DatatableColumn<ScheduledTask> = {
    id: 'schedule',
    header: $localize`:schedule column heading|Column heading for the dates a task is scheduled on:Schedule`,
    accessor: (task) => scheduleLabel(task),
    sortable: true,
    widthClass: 'w-52',
  };

  // The calendar endpoint names its status sort field statusName rather than
  // status, so the catalog column carries an endpoint-specific sort key here.
  readonly columns: DatatableColumn<ScheduledTask>[] = [
    ...taskColumns<ScheduledTask>(['systemId', 'name', 'project', 'status'], {
      overrides: {
        name: { cellClass: 'min-w-48' },
        status: { sortKey: 'statusName' },
      },
    }),
    this.scheduleColumn,
    ...taskColumns<ScheduledTask>(['assignees']),
  ];

  protected openTaskLabel(task: ScheduledTask): string {
    return `Open ${task.systemId}, ${task.name}`;
  }
}

const scheduleLabel = (task: ScheduledTask): string => {
  const start = taskStartsOn(task);
  const end = taskEndsOn(task);
  return start === end ? (start ?? 'Unscheduled') : `${start} – ${end}`;
};
