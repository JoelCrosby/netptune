import { Component, computed, inject, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { SprintStatus } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { removeTaskFromSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
} from '@static/components/datatable/datatable.types';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SprintBacklogPriorityClassPipe } from '../pipes/sprint-backlog-priority-class.pipe';
import { SprintBacklogPriorityLabelPipe } from '../pipes/sprint-backlog-priority-label.pipe';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';
import { SprintAddTaskFormComponent } from './sprint-add-task-form.component';

@Component({
  selector: 'app-sprint-task-list',
  imports: [
    RouterLink,
    StrokedButtonComponent,
    TaskScopeIdComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    SprintAddTaskFormComponent,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogStatusLabelPipe,
    SprintBacklogPriorityClassPipe,
    SprintBacklogPriorityLabelPipe,
  ],
  template: `
    @if (canEditSprintTasks()) {
      <app-sprint-add-task-form [sprintId]="sprint().id!" />
    }

    <app-datatable
      containerClass="overflow-auto"
      tableClass="min-w-[820px] table-fixed"
      rowClass="bg-card"
      emptyMessage="No tasks in this sprint."
      [data]="data()"
      [stickyHeader]="true">
      <ng-template appDatatableCell="systemId" let-task>
        <app-task-scope-id [id]="task.systemId" />
      </ng-template>

      <ng-template appDatatableCell="name" let-task>
        <a
          class="block w-full truncate font-medium hover:underline"
          [routerLink]="['../../tasks', task.systemId]">
          {{ task.name }}
        </a>
      </ng-template>

      <ng-template appDatatableCell="projectName" let-task>
        <span class="text-muted block truncate text-sm">
          {{ task.projectName }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="status" let-task>
        <span
          class="inline-flex items-center rounded px-2 py-0.5 text-center text-xs font-medium"
          [class]="task.statusCategory | sprintBacklogStatusBadgeClass">
          {{ task.statusName | sprintBacklogStatusLabel }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="priority" let-task>
        @if (task.priority !== null && task.priority !== undefined) {
          <span
            class="text-sm font-medium"
            [class]="task.priority | sprintBacklogPriorityClass">
            {{ task.priority | sprintBacklogPriorityLabel }}
          </span>
        } @else {
          <span class="text-muted text-sm">None</span>
        }
      </ng-template>

      <ng-template appDatatableCell="actions" let-task>
        <button
          app-stroked-button
          color="primary"
          type="button"
          class="h-6 text-xs"
          [disabled]="updateLoading()"
          (click)="onRemoveTask(task.id)">
          Remove
        </button>
      </ng-template>
    </app-datatable>
  `,
})
export class SprintTaskListComponent {
  private store = inject(Store);
  private hub = inject(ProjectTasksHubService);

  readonly sprint = input.required<SprintDetailViewModel>();
  readonly canManage = input.required<boolean>();

  readonly sprintStatus = SprintStatus;
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);
  readonly canEditSprintTasks = computed(
    () => this.canManage() && this.sprint().status !== SprintStatus.completed
  );

  private params = computed<Params>(() => ({ sprintId: this.sprint().id }));

  private readonly baseColumns: DatatableColumn<TaskViewModel>[] = [
    { id: 'systemId', header: 'Key', sortable: true, widthClass: 'w-28' },
    { id: 'name', header: 'Task', accessor: 'name', sortable: true },
    { id: 'projectName', header: 'Project', sortable: true, widthClass: 'w-48' },
    { id: 'status', header: 'Status', sortable: true, widthClass: 'w-40' },
    { id: 'priority', header: 'Priority', sortable: true, widthClass: 'w-32' },
  ];

  private readonly actionsColumn: DatatableColumn<TaskViewModel> = {
    id: 'actions',
    header: '',
    widthClass: 'w-28',
    align: 'end',
  };

  readonly data = computed<DatatableDataSource<TaskViewModel>>(() => ({
    key: 'sprint-tasks',
    columns: this.canEditSprintTasks()
      ? [...this.baseColumns, this.actionsColumn]
      : this.baseColumns,
    resource: {
      url: 'api/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    reloadSignal: this.hub.updateVersion,
  }));

  onRemoveTask(taskId?: number) {
    const sprintId = this.sprint().id;
    if (!sprintId || !taskId) return;
    this.store.dispatch(removeTaskFromSprint({ sprintId, taskId }));
  }
}
