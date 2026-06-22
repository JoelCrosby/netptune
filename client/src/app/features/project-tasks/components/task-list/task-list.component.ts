import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { Params } from '@angular/router';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { DatatableCellTemplateDirective } from '@app/static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@app/static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@app/static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableLoadParams,
} from '@app/static/components/datatable/datatable.types';
import { ClientResponse } from '@core/models/client-response';
import { Page } from '@core/models/pagination';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import * as actions from '@core/store/tasks/tasks.actions';
import {
  selectProjectTasksFilter,
  selectTaskFiltersActive,
  selectTasks,
  selectTasksPage,
  selectTasksPageSize,
  selectTasksTotalCount,
  selectTasksTotalPages,
} from '@core/store/tasks/tasks.selectors';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideArchiveRestore,
  LucideCheck,
  LucideListChecks,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TablePaginationComponent } from '@static/components/table/table.component';
import { TaskListFiltersComponent } from './task-list-filters.component';

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
    TablePaginationComponent,
    TaskListFiltersComponent,
  ],
  template: `
    <app-task-list-filters />

    <app-datatable
      containerClass="h-[calc(100vh-312px)] min-h-16 overflow-auto"
      tableClass="min-w-[760px] table-fixed"
      rowClass="bg-card"
      [data]="taskData"
      [selection]="canDelete()"
      [stickyHeader]="true">
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

      <div appDatatableEmpty class="flex justify-center">
        <div
          class="my-10 flex h-full flex-col items-center justify-center gap-2">
          <svg size="38" lucideListChecks></svg>
          <h4 class="mx-16 text-center font-normal">
            {{
              filtersActive()
                ? 'No tasks match these filters.'
                : 'There are currently no tasks.'
            }}
          </h4>

          @if (canCreate() && !filtersActive()) {
            <p class="text-foreground/70 mb-4 text-center text-sm">
              Use the Create Task button to create your first task and get
              started.
            </p>
            <button app-flat-button type="button" (click)="createTaskClicked()">
              <svg size="20" lucidePlus></svg>
              <span>Create Task</span>
            </button>
          }
        </div>
      </div>

      <app-table-pagination
        itemLabel="tasks"
        [page]="currentPage()"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[25, 50, 100]"
        [totalItems]="totalCount()"
        [totalPages]="totalPages()"
        (pageChange)="goToPage($event)"
        (pageSizeChange)="setPageSize($event)" />
    </app-datatable>
  `,
})
export class TaskListComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly tasks = this.store.selectSignal(selectTasks);
  readonly taskFilter = this.store.selectSignal(selectProjectTasksFilter);
  readonly filtersActive = this.store.selectSignal(selectTaskFiltersActive);
  readonly currentPage = this.store.selectSignal(selectTasksPage);
  readonly pageSize = this.store.selectSignal(selectTasksPageSize);
  readonly totalCount = this.store.selectSignal(selectTasksTotalCount);
  readonly totalPages = this.store.selectSignal(selectTasksTotalPages);
  readonly statusCategory = StatusCategory;

  readonly canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.create)
  );
  readonly canDelete = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.delete)
  );

  readonly taskData: DatatableDataSource<TaskViewModel> = {
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
    ],
    resource: (params, injector) =>
      httpResource<ClientResponse<Page<TaskViewModel>>>(
        () => ({
          url: 'api/tasks',
          params: this.taskRequestParams(params()),
        }),
        {
          injector,
          defaultValue: emptyTaskPageResponse,
        }
      ),
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    menu: [
      {
        label: 'Mark Complete',
        icon: LucideCheck,
        onClick: (task) => this.markCompleteClicked(task),
      },
      {
        label: 'Move to Backlog',
        icon: LucideArchiveRestore,
        onClick: (task) => this.moveToBacklogClicked(task),
      },
      {
        label: 'Delete',
        icon: LucideTrash2,
        onClick: (task) => this.deleteClicked(task),
      },
    ],
  };

  goToPage(page: number) {
    this.store.dispatch(actions.setProjectTasksPage({ page }));
  }

  setPageSize(pageSize: number) {
    this.store.dispatch(actions.setProjectTasksPageSize({ pageSize }));
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
      width: '600px',
    });
  }

  deleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.deleteProjectTask({
        identifier: `[workspace] ${task.workspaceKey}`,
        task,
      })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    const completeStatusId = this.findStatusId('complete');
    if (!completeStatusId) return;

    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${task.workspaceKey}`,
        task: {
          ...task,
          statusId: completeStatusId,
        },
      })
    );
  }

  moveToBacklogClicked(task: TaskViewModel) {
    const inactiveStatusId = this.findStatusId('inactive');
    if (!inactiveStatusId) return;

    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${task.workspaceKey}`,
        task: {
          ...task,
          statusId: inactiveStatusId,
        },
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

  private findStatusId(key: string): number | undefined {
    return this.tasks().find((task) => task.statusKey === key)?.statusId;
  }

  private taskRequestParams(loadParams: DatatableLoadParams) {
    const filter = this.taskFilter();
    const params: Params = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
    };

    if (filter.search) {
      params['search'] = filter.search;
    }

    if (filter.sprintId !== undefined) {
      params['sprintId'] = filter.sprintId;
    }

    if (filter.noSprint !== undefined) {
      params['noSprint'] = filter.noSprint;
    }

    if (filter.tags?.length) {
      params['tags'] = filter.tags;
    }

    if (filter.statusIds?.length) {
      params['statusIds'] = filter.statusIds;
    }

    if (filter.assignees?.length) {
      params['assignees'] = filter.assignees;
    }

    if (loadParams.sort) {
      params['sortBy'] = loadParams.sort.field;
      params['sortDirection'] = loadParams.sort.direction;
    }

    return params;
  }
}

const emptyTaskPageResponse: ClientResponse<Page<TaskViewModel>> = {
  isSuccess: true,
  payload: {
    items: [],
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 1,
  },
};
