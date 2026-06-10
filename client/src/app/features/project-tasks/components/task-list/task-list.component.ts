import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { IconButtonComponent } from '@app/static/components/button/icon-button.component';
import { TaskStatus } from '@core/enums/project-task-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import * as actions from '@core/store/tasks/tasks.actions';
import {
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
  LucideEllipsisVertical,
  LucideListChecks,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TablePaginationComponent,
  TableRowDirective,
} from '@static/components/table/table.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskStatusPipe } from '@static/pipes/task-status.pipe';
import { TaskListFiltersComponent } from './task-list-filters.component';

@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FlatButtonComponent,
    IconButtonComponent,
    LucideArchiveRestore,
    LucideCheck,
    LucideEllipsisVertical,
    LucideListChecks,
    LucidePlus,
    LucideTrash2,
    AvatarComponent,
    CheckboxComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    SprintBadgeComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TablePaginationComponent,
    TableRowDirective,
    TaskListFiltersComponent,
    TaskStatusPipe,
  ],
  template: `
    <app-task-list-filters />

    <app-table
      containerClass="h-[calc(100vh-312px)] min-h-16 overflow-auto"
      tableClass="min-w-[760px] table-fixed">
      <thead appTableHead [sticky]="true">
        <tr appTableHeaderRow>
          @if (canDelete()) {
            <th class="w-10 px-2 py-3"></th>
            <th class="w-10 px-2 py-3"></th>
          }
          <th class="w-28 px-4 py-3">Key</th>
          <th class="px-4 py-3">Task</th>
          <th class="w-38 px-4 py-3">Sprint</th>
          <th class="w-48 px-4 py-3">Status</th>
          <th class="w-40 px-4 py-3">Assignees</th>
        </tr>
      </thead>
      <tbody>
        @for (task of tasks(); track task.id) {
          <tr appTableRow class="bg-card">
            @if (canDelete()) {
              <td class="px-2 align-middle">
                <button
                  class="w-8"
                  app-icon-button
                  type="button"
                  aria-label="Task actions"
                  (click)="menu.toggle($any($event.currentTarget))">
                  <svg
                    lucideEllipsisVertical
                    class="text-foreground/30 h-4 w-4"></svg>
                </button>

                <app-dropdown-menu #menu xPosition="after">
                  <button
                    app-menu-item
                    (click)="markCompleteClicked(task); menu.close()">
                    <svg lucideCheck class="h-4 w-4"></svg>
                    <span>Mark Complete</span>
                  </button>

                  <button
                    app-menu-item
                    (click)="moveToBacklogClicked(task); menu.close()">
                    <svg lucideArchiveRestore class="h-4 w-4"></svg>
                    <span>Move to Backlog</span>
                  </button>

                  <button
                    app-menu-item
                    (click)="deleteClicked(task); menu.close()">
                    <svg lucideTrash2 class="h-4 w-4"></svg>
                    <span>Delete</span>
                  </button>
                </app-dropdown-menu>
              </td>
              <td class="px-2 py-2 align-middle">
                <app-checkbox />
              </td>
            }

            <td class="px-4 py-2.5 align-middle">
              <span
                class="bg-foreground/10 inline rounded px-1.5 py-0.5 text-sm">
                {{ task.systemId }}
              </span>
            </td>
            <td class="min-w-64 px-4 py-2.5 align-middle">
              <button
                class="block w-full cursor-pointer truncate text-left font-medium hover:underline"
                type="button"
                (click)="titleClicked(task)">
                {{ task.name }}
              </button>
            </td>
            <td class="px-4 py-2.5 align-middle">
              @if (task.sprintName) {
                <app-sprint-badge
                  class="max-w-40"
                  [name]="task.sprintName"
                  [status]="task.sprintStatus" />
              } @else {
                <span class="text-muted text-sm">Backlog</span>
              }
            </td>
            <td class="px-4 py-2.5 align-middle">
              <span
                class="inline-flex items-center gap-1.5 rounded px-2 py-0.5 text-center text-xs font-medium"
                [class]="statusBadgeClass(task.status)">
                @if (task.status === taskStatus.complete) {
                  <svg lucideCheck class="h-3.5 w-3.5"></svg>
                }
                {{ task.status | taskStatus }}
              </span>
            </td>
            <td class="px-4 py-2.5 align-middle">
              @if (task.assignees.length) {
                <div
                  class="flex max-w-32 items-center -space-x-2 overflow-hidden">
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
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell [attr.colspan]="columnCount()">
              <div class="flex justify-center">
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
                      Use the Create Task button to create your first task and
                      get started.
                    </p>
                    <button
                      app-flat-button
                      type="button"
                      (click)="createTaskClicked()">
                      <svg size="20" lucidePlus></svg>
                      <span>Create Task</span>
                    </button>
                  }
                </div>
              </div>
            </td>
          </tr>
        }
      </tbody>

      <app-table-pagination
        itemLabel="tasks"
        [page]="currentPage()"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[25, 50, 100]"
        [totalItems]="totalCount()"
        [totalPages]="totalPages()"
        (pageChange)="goToPage($event)"
        (pageSizeChange)="setPageSize($event)" />
    </app-table>
  `,
})
export class TaskListComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly tasks = this.store.selectSignal(selectTasks);
  readonly filtersActive = this.store.selectSignal(selectTaskFiltersActive);
  readonly currentPage = this.store.selectSignal(selectTasksPage);
  readonly pageSize = this.store.selectSignal(selectTasksPageSize);
  readonly totalCount = this.store.selectSignal(selectTasksTotalCount);
  readonly totalPages = this.store.selectSignal(selectTasksTotalPages);
  readonly taskStatus = TaskStatus;

  readonly canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.create)
  );
  readonly canDelete = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.delete)
  );

  goToPage(page: number) {
    this.store.dispatch(actions.setProjectTasksPage({ page }));
  }

  setPageSize(pageSize: number) {
    this.store.dispatch(actions.setProjectTasksPageSize({ pageSize }));
  }

  columnCount(): number {
    return this.canDelete() ? 7 : 5;
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
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${task.workspaceKey}`,
        task: {
          ...task,
          status: TaskStatus.complete,
        },
      })
    );
  }

  moveToBacklogClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${task.workspaceKey}`,
        task: {
          ...task,
          status: TaskStatus.inActive,
        },
      })
    );
  }

  statusBadgeClass(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.new:
        return 'bg-blue-100 text-blue-700';
      case TaskStatus.inProgress:
        return 'bg-yellow-100 text-yellow-700';
      case TaskStatus.complete:
        return 'bg-green-100 text-green-700';
      case TaskStatus.onHold:
        return 'bg-purple-100 text-purple-700';
      default:
        return 'bg-neutral-100 text-neutral-600';
    }
  }
}
