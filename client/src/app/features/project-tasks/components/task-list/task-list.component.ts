import {
  Component,
  computed,
  effect,
  inject,
  output,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Params } from '@angular/router';
import { PERMISSONS } from '@app/core/auth/permissions';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { DatatableCellTemplateDirective } from '@app/static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@app/static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@app/static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableMenuItem,
} from '@app/static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@app/static/components/empty-state/empty-state.component';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskSelectionService } from '@core/services/task-selection.service';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideListChecks,
  LucideMessageSquareText,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { AvatarStackComponent } from '@static/components/avatar-stack/avatar-stack.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskFlagBadgeComponent } from '@static/components/task-flag-badge.component';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';
import { TaskListFiltersComponent } from './task-list-filters.component';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { ProjectTasksHubService } from '@app/core/store/tasks/tasks.hub.service';
import { DatePipe } from '@angular/common';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';

@Component({
  selector: 'app-task-list',
  imports: [
    FlatButtonComponent,
    LucideListChecks,
    LucidePlus,
    LucideMessageSquareText,
    AvatarStackComponent,
    TaskStatusPillComponent,
    SprintBadgeComponent,
    TaskScopeIdComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    TaskListFiltersComponent,
    DatePipe,
    TooltipDirective,
    TaskFlagBadgeComponent,
  ],
  providers: [],
  template: `
    <app-task-list-filters />

    <app-datatable
      i18n-errorMessage="Shown when the task list fails to load"
      errorMessage="Tasks could not be loaded."
      #datatable
      containerClass="h-[calc(100vh-312px)] min-h-160 overflow-auto"
      tableClass="min-w-[760px] table-fixed"
      [data]="taskData()"
      [selection]="canDelete()"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (selectionChanged)="onSelectionChanged($event)"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableCell="systemId" let-task>
        <app-task-scope-id [id]="task.systemId" />
      </ng-template>

      <ng-template appDatatableCell="name" let-task>
        <button
          class="block flex w-full cursor-pointer items-center gap-2 truncate text-left font-medium hover:underline"
          type="button"
          (click)="titleClicked(task)">
          {{ task.name }}
          @if (task.hasComments) {
            <svg
              lucideMessageSquareText
              class="text-muted h-4 w-4"
              i18n-aria-label="
                Accessible label for the icon marking a task that has comments
              "
              aria-label="Has comments"
              i18n-appTooltip="
                Tooltip on the icon marking a task that has comments
              "
              appTooltip="Has comments"></svg>
          }
          @if (readFlags()) {
            <app-task-flag-badge
              [count]="task.flags.length"
              [names]="flagNames(task)" />
          }
        </button>
      </ng-template>

      <ng-template appDatatableCell="sprint" let-task>
        @if (task.sprintName) {
          <app-sprint-badge
            class="max-w-40"
            [name]="task.sprintName"
            [status]="task.sprintStatus" />
        } @else {
          <span
            class="text-muted text-sm"
            i18n="Shown when a task is not in any sprint">
            Backlog
          </span>
        }
      </ng-template>

      <ng-template appDatatableCell="status" let-task>
        <app-task-status-pill
          [name]="task.statusName"
          [color]="task.statusColor"
          [category]="task.statusCategory" />
      </ng-template>

      <ng-template appDatatableCell="assignees" let-task>
        @if (task.assignees.length) {
          <app-avatar-stack [avatars]="task.assignees" />
        } @else {
          <span
            class="text-muted text-sm"
            i18n="Shown when a task has nobody assigned">
            Unassigned
          </span>
        }
      </ng-template>

      <ng-template appDatatableCell="updatedAt" let-task>
        <span
          class="text-muted text-sm"
          [appTooltip]="task.updatedAt | date: 'medium'">
          {{ task.updatedAt | date }}
        </span>
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
            <span i18n="Button that opens the create-task dialog">
              Create Task
            </span>
          </button>
        }
      </app-empty-state>
    </app-datatable>
  `,
})
export class TaskListComponent {
  private dialog = inject(DialogService);
  private projectTasksHubService = inject(ProjectTasksHubService);

  private readonly sprintFilter = inject(SprintFilterService);

  private datatable = viewChild(DatatableComponent<TaskViewModel>);

  readonly countChange = output<number>();

  private readonly taskCommands = inject(TaskCommandsService);
  private readonly taskSelection = inject(TaskSelectionService);

  selection = this.taskSelection.taskIds;

  private readonly filterRoute = taskFilterRoute();

  readonly filtersActive = computed(() => {
    const routeFilters = this.filterRoute.filters();

    const presenceFiltersActive =
      routeFilters.hasFlags === true || routeFilters.hasTags !== undefined;

    return (
      this.filterRoute.hasFilters() ||
      presenceFiltersActive ||
      this.sprintFilter.sprintId() !== undefined
    );
  });

  canCreate = hasPermission(PERMISSONS.tasks.create);
  canDelete = hasPermission(PERMISSONS.tasks.delete);
  readFlags = hasPermission(PERMISSONS.flags.read);

  taskRequestParams = computed(() => {
    const filters = this.filterRoute.filters();
    const queryParams: Params = { ...filters };
    const search = filters.term?.trim();

    if (search) {
      queryParams['search'] = search;
    }

    const sprintId = this.sprintFilter.sprintId();

    if (sprintId !== undefined) {
      queryParams['sprintId'] = sprintId;
    }

    if (filters.statuses?.length) {
      queryParams['statusIds'] = filters.statuses;
    }

    if (filters.users?.length) {
      queryParams['assignees'] = filters.users;
    }

    return queryParams;
  });

  flagNames(task: TaskViewModel): string[] {
    return task.flags.map((flag) => flag.name);
  }

  private readonly deleteMenuItem: DatatableMenuItem<TaskViewModel> = {
    label: $localize`:Row action that deletes a task:Delete`,
    icon: LucideTrash2,
    onClick: (task) => this.deleteClicked(task),
  };

  readonly taskData = computed<DatatableDataSource<TaskViewModel>>(() => ({
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
    menu: this.canDelete() ? [this.deleteMenuItem] : [],
    reloadSignal: this.projectTasksHubService.updateVersion,
  }));

  constructor() {
    this.taskSelection.clear();

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
    this.taskSelection.set(tasks.map((task) => task.id));
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
    this.taskCommands.delete(task);
  }
}
