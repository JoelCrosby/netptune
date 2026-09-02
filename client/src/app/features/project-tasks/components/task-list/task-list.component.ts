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
import { PERMISSIONS } from '@app/core/auth/permissions';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { DatatableEmptyDirective } from '@app/static/components/datatable/datatable-empty.directive';
import {
  DatatableColumn,
  DatatableMenuItem,
} from '@app/static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@app/static/components/empty-state/empty-state.component';
import { TaskTableComponent } from '@app/static/components/task-table.component';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskSelectionService } from '@core/services/task-selection.service';
import { taskColumns, taskNameCell } from '@core/tasks/task-columns';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { LucideListChecks, LucidePlus, LucideTrash2 } from '@lucide/angular';
import { taskFilterRoute } from '@core/router/task-filter-route';

@Component({
  selector: 'app-task-list',
  imports: [
    FlatButtonComponent,
    LucideListChecks,
    LucidePlus,
    DatatableEmptyDirective,
    EmptyStateComponent,
    TaskTableComponent,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <app-task-table
      #table
      i18n-errorMessage="Shown when the task list fails to load"
      errorMessage="Tasks could not be loaded."
      key="task-list"
      url="api/tasks"
      tableClass="min-w-[760px] table-fixed"
      [autoFill]="true"
      [columns]="columns()"
      [params]="taskRequestParams"
      [menu]="menu()"
      [selection]="canDelete()"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (selectionChanged)="onSelectionChanged($event)"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableEmpty>
        <app-empty-state
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
      </ng-template>
    </app-task-table>
  `,
})
export class TaskListComponent {
  private dialog = inject(DialogService);

  private readonly sprintFilter = inject(SprintFilterService);

  private table = viewChild(TaskTableComponent<TaskViewModel>);

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

  canCreate = hasPermission(PERMISSIONS.tasks.create);
  canDelete = hasPermission(PERMISSIONS.tasks.delete);
  readFlags = hasPermission(PERMISSIONS.flags.read);

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

  readonly columns = computed<DatatableColumn<TaskViewModel>[]>(() => {
    const readFlags = this.readFlags();

    return taskColumns<TaskViewModel>(
      [
        'systemId',
        'name',
        'sprint',
        'status',
        'priority',
        'assignees',
        'updatedAt',
      ],
      {
        overrides: {
          name: taskNameCell<TaskViewModel>({
            action: (task) => this.titleClicked(task),
            showComments: true,
            flagNames: (task) => {
              if (!readFlags) return [];

              return task.flags.map((flag) => flag.name);
            },
          }),
          sprint: { widthClass: 'w-38' },
          status: { widthClass: 'w-48' },
        },
      }
    );
  });

  private readonly deleteMenuItem: DatatableMenuItem<TaskViewModel> = {
    label: $localize`:Row action that deletes a task:Delete`,
    icon: LucideTrash2,
    onClick: (task) => this.deleteClicked(task),
  };

  readonly menu = computed<DatatableMenuItem<TaskViewModel>[]>(() => {
    return this.canDelete() ? [this.deleteMenuItem] : [];
  });

  constructor() {
    this.taskSelection.clear();

    effect(() => {
      if (this.selection().length === 0) {
        this.table()?.clearSelection();
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
      height: TaskDetailDialogComponent.height,
      data: task,
      autoFocus: false,
      panelClass: TaskDetailDialogComponent.panelClass,
    });
  }

  createTaskClicked() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
      height: CreateTaskDialogComponent.height,
      panelClass: CreateTaskDialogComponent.panelClass,
    });
  }

  deleteClicked(task: TaskViewModel) {
    this.taskCommands.delete(task);
  }
}
