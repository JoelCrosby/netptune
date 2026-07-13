import {
  Component,
  computed,
  effect,
  inject,
  output,
  viewChild,
} from '@angular/core';
import { Params } from '@angular/router';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { DatatableCellTemplateDirective } from '@app/static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@app/static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@app/static/components/datatable/datatable.component';
import { DatatableDataSource } from '@app/static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@app/static/components/empty-state/empty-state.component';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import * as actions from '@core/store/tasks/tasks.actions';
import {
  selectProjectTasksFilter,
  selectSelectedTaskIds,
  selectTaskFiltersActive,
} from '@core/store/tasks/tasks.selectors';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideCheck,
  LucideListChecks,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskListFiltersComponent } from './task-list-filters.component';
import { injectParams } from '@app/core/router/signals';
import { parseTaskFilterRouteParams } from '@app/core/router/task-filter-route-params';
import { ProjectTasksHubService } from '@app/core/store/tasks/tasks.hub.service';
import { DatePipe } from '@angular/common';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';

@Component({
  selector: 'app-task-list',
  imports: [
    FlatButtonComponent,
    LucideCheck,
    LucideListChecks,
    LucidePlus,
    AvatarComponent,
    SprintBadgeComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    TaskListFiltersComponent,
    DatePipe,
    TooltipDirective,
  ],
  providers: [],
  template: `
    <app-task-list-filters />

    <app-datatable
      #datatable
      containerClass="h-[calc(100vh-312px)] min-h-160 overflow-auto"
      tableClass="min-w-[760px] table-fixed"
      rowClass="bg-card"
      [data]="taskData"
      [selection]="canDelete()"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (selectionChanged)="onSelectionChanged($event)"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableCell="systemId" let-task>
        <span class="bg-foreground/10 inline rounded px-1.5 py-0.5 text-sm">
          {{ task.systemId }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="name" let-task>
        <button
          class="block w-full cursor-pointer truncate text-left font-medium hover:underline"
          type="button"
          (click)="titleClicked(task)">
          {{ task.name }}
        </button>
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
          class="inline-flex items-center gap-1.5 rounded px-2 py-0.5 text-center text-xs font-medium"
          [class]="statusBadgeClass(task.statusCategory)">
          @if (task.statusCategory === statusCategory.done) {
            <svg lucideCheck class="h-3.5 w-3.5"></svg>
          }
          {{ task.statusName }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="assignees" let-task>
        @if (task.assignees.length) {
          <div class="flex max-w-32 items-center -space-x-2 overflow-hidden">
            @for (assignee of task.assignees; track assignee.id) {
              <app-avatar
                class="ring-card rounded-full ring-2"
                size="sm"
                [name]="assignee.displayName"
                [imageUrl]="assignee.pictureUrl" />
            }
          </div>
        } @else {
          <span class="text-muted text-sm">Unassigned</span>
        }
      </ng-template>

      <ng-template appDatatableCell="updatedAt" let-task>
        <span
          class="text-muted text-sm"
          [appTooltip]="task.updatedAt | date: 'medium'"
          >{{ task.updatedAt | date }}</span
        >
      </ng-template>

      <app-empty-state
        appDatatableEmpty
        [title]="
          filtersActive()
            ? 'No tasks match these filters.'
            : 'There are currently no tasks.'
        "
        [description]="
          filtersActive()
            ? ''
            : 'Use the Create Task button to create your first task and get started.'
        ">
        <svg emptyStateIcon size="38" lucideListChecks></svg>

        @if (canCreate() && !filtersActive()) {
          <button
            emptyStateAction
            app-flat-button
            type="button"
            (click)="createTaskClicked()">
            <svg size="20" lucidePlus></svg>
            <span>Create Task</span>
          </button>
        }
      </app-empty-state>
    </app-datatable>
  `,
})
export class TaskListComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);
  private projectTasksHubService = inject(ProjectTasksHubService);
  private params = injectParams();

  private datatable = viewChild(DatatableComponent<TaskViewModel>);

  readonly countChange = output<number>();

  selection = this.store.selectSignal(selectSelectedTaskIds);

  taskFilter = this.store.selectSignal(selectProjectTasksFilter);
  filtersActive = this.store.selectSignal(selectTaskFiltersActive);

  statusCategory = StatusCategory;

  canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.create)
  );
  canDelete = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.delete)
  );

  taskRequestParams = computed(() => {
    const params = this.params();
    const filter = this.taskFilter();
    const filters = parseTaskFilterRouteParams(params);
    const queryParams: Params = { ...filters };

    if (filter.search) {
      queryParams['search'] = filter.search;
    }

    if (filter.sprintId !== undefined) {
      queryParams['sprintId'] = filter.sprintId;
    }

    if (filter.noSprint !== undefined) {
      queryParams['noSprint'] = filter.noSprint;
    }

    if (filter.tags?.length) {
      queryParams['tags'] = filter.tags;
    }

    if (filter.statusIds?.length) {
      queryParams['statusIds'] = filter.statusIds;
    }

    if (filter.assignees?.length) {
      queryParams['assignees'] = filter.assignees;
    }

    return queryParams;
  });

  taskData: DatatableDataSource<TaskViewModel> = {
    key: 'task-list',
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
        cellClass: 'min-w-64',
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
        widthClass: 'w-48',
      },
      {
        id: 'assignees',
        header: 'Assignees',
        sortKey: 'assignees',
        widthClass: 'w-40',
      },
      {
        id: 'updatedAt',
        header: 'Updated',
        sortKey: 'updatedAt',
        widthClass: 'w-40',
      },
    ],
    resource: {
      url: 'api/tasks',
      params: this.taskRequestParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    menu: [
      {
        label: 'Delete',
        icon: LucideTrash2,
        onClick: (task) => this.deleteClicked(task),
      },
    ],
    reloadSignal: this.projectTasksHubService.updateVersion,
  };

  constructor() {
    // Start each visit to the list with a clean selection; the datatable's
    // internal selection is recreated fresh on every mount.
    this.store.dispatch(actions.clearSelectedTaskIds());

    // Keep the datatable's internal selection in sync when the store
    // selection is cleared elsewhere (e.g. after a bulk delete).
    effect(() => {
      if (this.selection().length === 0) {
        this.datatable()?.clearSelection();
      }
    });
  }

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.countChange.emit(event.totalCount);
    }
  }

  onSelectionChanged(tasks: TaskViewModel[]) {
    this.store.dispatch(
      actions.setSelectedTaskIds({ ids: tasks.map((e) => e.id) })
    );
  }

  titleClicked(task: TaskViewModel) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: task,
      autoFocus: false,
      panelClass: 'app-modal-class',
    });
  }

  createTaskClicked() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
    });
  }

  deleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.deleteProjectTask.init({
        identifier: `[workspace] ${task.workspaceKey}`,
        task,
      })
    );
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
