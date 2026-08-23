import { Component, computed, inject, input, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Params } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { taskColumns, taskNameCell } from '@core/tasks/task-columns';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { scrollHeights } from '@static/components/datatable/datatable-classes';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskTableComponent } from '@static/components/task-table.component';

@Component({
  selector: 'app-sprint-backlog-group',
  imports: [
    BadgeComponent,
    DropdownButtonComponent,
    MenuItemComponent,
    DatatableCellTemplateDirective,
    TaskTableComponent,
  ],
  template: `
    <div class="flex flex-col gap-2" [class.hidden]="isEmpty()">
      <div class="flex items-center gap-2">
        <h2 class="pl-2 font-semibold tracking-wide uppercase">
          {{ label() }}
        </h2>
        <app-badge color="primary">{{ count() }}</app-badge>
      </div>

      <div class="p-2">
        <app-task-table
          key="sprint-backlog"
          url="api/sprints/backlog"
          tableClass="min-w-[1040px] table-fixed"
          [containerClass]="scrollHeights.panel"
          [columns]="columns()"
          [params]="params"
          [stickyHeader]="true"
          (loaded)="onLoaded($event)">
          <ng-template appDatatableCell="assign" let-task>
            @if (sprints().length > 0) {
              <app-dropdown-button
                #assignMenu
                i18n-label="Button that assigns the task to a sprint"
                label="Assign to sprint"
                buttonClass="w-42 h-7 text-xs justify-between"
                color="neutral"
                xPosition="before"
                [disabled]="loading()">
                @for (sprint of sprints(); track sprint.id) {
                  <button
                    app-menu-item
                    type="button"
                    class="min-w-52"
                    (click)="onAssign(task, sprint.id); assignMenu.close()">
                    <span class="flex min-w-0 flex-col items-start">
                      <span class="max-w-48 truncate font-medium">
                        {{ sprint.name }}
                      </span>
                      <span class="text-muted max-w-48 truncate text-xs">
                        {{ sprint.projectName }}
                      </span>
                    </span>
                  </button>
                }
              </app-dropdown-button>
            } @else {
              <span
                class="text-muted text-sm"
                i18n="Shown when there is no sprint to assign a task to">
                No sprints available
              </span>
            }
          </ng-template>
        </app-task-table>
      </div>
    </div>
  `,
})
export class SprintBacklogGroupComponent {
  readonly scrollHeights = scrollHeights;

  readonly label = input.required<string>();
  readonly categories = input.required<number[]>();
  readonly filterParams = input.required<Params>();
  readonly sprints = input.required<SprintViewModel[]>();

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly loading = this.sprintCommands.isUpdating;
  readonly canManageTasks = hasPermission(PERMISSIONS.sprints.manageTasks);

  // Total backlog tasks for this group and whether its fetch has resolved,
  // pushed up from the table's own paginated fetch via its (loaded) output.
  private totalCount = signal(0);
  private resolved = signal(false);
  readonly count = this.totalCount.asReadonly();
  readonly hasLoaded = this.resolved.asReadonly();
  // Hide the whole group once we know it has no tasks. The table stays mounted
  // (hidden, not removed) so it keeps refetching when filters change.
  readonly isEmpty = computed(() => this.resolved() && this.totalCount() === 0);

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    this.totalCount.set(event.totalCount);
    this.resolved.set(event.hasValue);
  }

  readonly params = computed<Params>(() => ({
    statusCategories: this.categories(),
    ...this.filterParams(),
  }));

  private readonly baseColumns = taskColumns<TaskViewModel>(
    ['systemId', 'name', 'status', 'priority', 'project', 'assignees'],
    {
      overrides: {
        name: taskNameCell<TaskViewModel>({
          link: (task) => ['../../tasks', task.systemId],
        }),
        status: { widthClass: 'w-32' },
        priority: { widthClass: 'w-24' },
        project: { widthClass: 'w-32' },
        assignees: { widthClass: 'w-28' },
      },
    }
  );

  private readonly assignColumn: DatatableColumn<TaskViewModel> = {
    id: 'assign',
    header: $localize`:Column heading for the sprint assign action:Assign`,
    widthClass: 'w-58',
  };

  readonly columns = computed<DatatableColumn<TaskViewModel>[]>(() => {
    return this.canManageTasks()
      ? [...this.baseColumns, this.assignColumn]
      : this.baseColumns;
  });

  onAssign(task: TaskViewModel, sprintId: number) {
    if (!task.id || !sprintId) return;

    this.sprintCommands.addTask(sprintId, task.id);
  }
}
