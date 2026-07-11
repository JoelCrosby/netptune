import { Component, computed, inject, signal } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { selectCurrentUserId } from '@core/store/auth/auth.selectors';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { Store } from '@ngrx/store';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
} from '@static/components/datatable/datatable.types';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { taskStatusBadgeClass } from '../utils/task-status-badge-class';

@Component({
  selector: 'app-dashboard-assigned-tasks',
  imports: [
    RouterLink,
    DatatableComponent,
    DatatableCellTemplateDirective,
    TaskScopeIdComponent,
    SprintBadgeComponent,
  ],
  template: `
    <section class="flex flex-col gap-3">
      <h2 class="text-foreground flex items-center gap-2 text-lg font-semibold">
        Assigned to me
        <span class="text-muted text-sm font-normal">{{ totalCount() }}</span>
      </h2>

      <app-datatable
        containerClass="h-[calc(100vh-612px)] min-h-80 overflow-auto"
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
  `,
})
export class DashboardAssignedTasksComponent {
  private store = inject(Store);
  private hub = inject(ProjectTasksHubService);

  readonly totalCount = signal<number | null>(null);

  readonly currentUserId = this.store.selectSignal(selectCurrentUserId);

  readonly statusBadgeClass = taskStatusBadgeClass;

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
}
