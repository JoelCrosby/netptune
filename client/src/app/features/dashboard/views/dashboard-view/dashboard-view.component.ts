import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import { selectCurrentUserId } from '@core/store/auth/auth.selectors';
import { StatusCategory } from '@core/models/status';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskStatusBreakdown } from '@core/models/view-models/task-status-breakdown';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { Store } from '@ngrx/store';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
} from '@static/components/datatable/datatable.types';
import {
  DonutStatCardComponent,
  DonutStatItem,
} from '@static/components/donut-stat-card/donut-stat-card.component';
import { DashboardNotificationsCardComponent } from '../../components/dashboard-notifications-card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';

// Distinct fallback hues for statuses that have no colour configured. Indexed
// by position so two statuses (even in the same category) never collapse to the
// same slice colour the way a category-keyed palette would.
const fallbackPalette = [
  '#3b82f6',
  '#22c55e',
  '#eab308',
  '#a855f7',
  '#ec4899',
  '#06b6d4',
  '#f97316',
  '#14b8a6',
  '#6366f1',
  '#ef4444',
];

@Component({
  selector: 'app-dashboard-view',
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DonutStatCardComponent,
    DashboardNotificationsCardComponent,
    TaskScopeIdComponent,
    SprintBadgeComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Dashboard" />

      <div class="flex flex-col gap-8">
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <app-donut-stat-card
            title="Tasks by status"
            totalLabel="Total"
            emptyMessage="No tasks to display."
            [items]="statusItems()"
            [total]="statusTotal()" />

          <app-dashboard-notifications-card class="lg:relative" />
        </div>

        <section class="flex flex-col gap-3">
          <h2
            class="text-foreground flex items-center gap-2 text-lg font-semibold">
            Assigned to me
            <span class="text-muted text-sm font-normal">{{
              totalCount()
            }}</span>
          </h2>

          <app-datatable
            containerClass="h-[calc(100vh-612px)] min-h-16 overflow-auto"
            tableClass="min-w-[820px] table-fixed"
            rowClass="bg-card"
            emptyMessage="You have no tasks assigned to you."
            [data]="data()"
            [stickyHeader]="true"
            (loaded)="onLoaded($event)">
            <ng-template appDatatableCell="systemId" let-task>
              <app-task-scope-id [id]="task.systemId" />
            </ng-template>

            <ng-template appDatatableCell="name" let-task>
              <a
                class="block w-full truncate font-medium hover:underline"
                [routerLink]="['../tasks', task.systemId]">
                {{ task.name }}
              </a>
            </ng-template>

            <ng-template appDatatableCell="projectName" let-task>
              <span class="text-muted block truncate text-sm">
                {{ task.projectName }}
              </span>
            </ng-template>

            <ng-template appDatatableCell="sprint" let-task>
              @if (task.sprintName) {
                <app-sprint-badge
                  class="max-w-40"
                  [name]="task.sprintName"
                  [status]="task.sprintStatus" />
              } @else {
                <span class="text-muted text-sm">Backlog</span>
              }
            </ng-template>

            <ng-template appDatatableCell="status" let-task>
              <span
                class="inline-flex items-center rounded px-2 py-0.5 text-center text-xs font-medium"
                [class]="statusBadgeClass(task.statusCategory)">
                {{ task.statusName }}
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
          </app-datatable>
        </section>
      </div>
    </app-page-container>
  `,
})
export class DashboardViewComponent {
  private store = inject(Store);
  private hub = inject(ProjectTasksHubService);

  readonly statusCategory = StatusCategory;
  readonly totalCount = signal<number | null>(null);

  readonly currentUserId = this.store.selectSignal(selectCurrentUserId);

  private readonly breakdown = httpResource<
    ClientResponse<TaskStatusBreakdown>
  >(() => 'api/tasks/status-breakdown');

  readonly statusItems = computed<DonutStatItem[]>(() =>
    (this.breakdown.value()?.payload?.statuses ?? []).map((status, index) => ({
      label: status.name,
      value: status.count,
      color:
        status.color?.trim() || fallbackPalette[index % fallbackPalette.length],
    }))
  );

  readonly statusTotal = computed(
    () => this.breakdown.value()?.payload?.total ?? 0
  );

  private params = computed<Params>(() => {
    const userId = this.currentUserId();
    return userId ? { assignees: [userId] } : {};
  });

  private readonly columns: DatatableColumn<TaskViewModel>[] = [
    { id: 'systemId', header: 'Key', sortable: true, widthClass: 'w-28' },
    { id: 'name', header: 'Task', accessor: 'name', sortable: true },
    {
      id: 'projectName',
      header: 'Project',
      sortKey: 'projectName',
      widthClass: 'w-48',
    },
    {
      id: 'sprint',
      header: 'Sprint',
      sortKey: 'sprintName',
      widthClass: 'w-38',
    },
    {
      id: 'status',
      header: 'Status',
      sortKey: 'statusName',
      widthClass: 'w-40',
    },
    { id: 'priority', header: 'Priority', sortable: true, widthClass: 'w-32' },
  ];

  readonly data = computed<DatatableDataSource<TaskViewModel>>(() => ({
    key: 'dashboard-assigned-tasks',
    columns: this.columns,
    resource: {
      url: 'api/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    reloadSignal: this.hub.updateVersion,
  }));

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.totalCount.set(event.totalCount);
    }
  }

  priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabels[priority];
  }

  priorityColor(priority: TaskPriority): string {
    return taskPriorityColors[priority];
  }

  statusBadgeClass(status: StatusCategory): string {
    switch (status) {
      case StatusCategory.todo:
        return 'bg-blue-100 text-blue-700';
      case StatusCategory.active:
        return 'bg-yellow-100 text-yellow-700';
      case StatusCategory.done:
        return 'bg-green-100 text-green-700';
      case StatusCategory.backlog:
        return 'bg-purple-100 text-purple-700';
      default:
        return 'bg-neutral-100 text-neutral-600';
    }
  }
}
