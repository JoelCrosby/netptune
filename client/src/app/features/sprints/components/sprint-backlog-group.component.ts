import { Component, computed, inject, input, signal } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { assignBacklogTask } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SprintBacklogPriorityClassPipe } from '../pipes/sprint-backlog-priority-class.pipe';
import { SprintBacklogPriorityLabelPipe } from '../pipes/sprint-backlog-priority-label.pipe';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';

@Component({
  selector: 'app-sprint-backlog-group',
  imports: [
    SprintBacklogStatusLabelPipe,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogPriorityLabelPipe,
    SprintBacklogPriorityClassPipe,
    RouterLink,
    AvatarComponent,
    BadgeComponent,
    DropdownButtonComponent,
    MenuItemComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    TaskScopeIdComponent,
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
        <app-datatable
          containerClass="h-[calc(100vh-912px)] min-h-80 overflow-auto"
          tableClass="min-w-[1040px] table-fixed"
          rowClass="bg-card"
          [data]="data"
          [stickyHeader]="true"
          (loaded)="onLoaded($event)">
          <ng-template appDatatableCell="name" let-task>
            <div class="flex min-w-0 items-center gap-2">
              <app-task-scope-id
                class="text-xs2 flex-none"
                [id]="task.systemId" />
              <a
                class="block min-w-0 truncate font-medium"
                [routerLink]="['../../tasks', task.systemId]">
                {{ task.name }}
              </a>
            </div>
          </ng-template>

          <ng-template appDatatableCell="status" let-task>
            <span
              class="truncate rounded px-1.5 py-0.5 text-xs font-medium"
              [class]="task.statusCategory | sprintBacklogStatusBadgeClass">
              {{ task.statusName | sprintBacklogStatusLabel }}
            </span>
          </ng-template>

          <ng-template appDatatableCell="priority" let-task>
            @if (task.priority !== null && task.priority !== undefined) {
              <span
                class="text-xs font-medium"
                [class]="task.priority | sprintBacklogPriorityClass">
                {{ task.priority | sprintBacklogPriorityLabel }}
              </span>
            } @else {
              <span class="text-muted text-xs">None</span>
            }
          </ng-template>

          <ng-template appDatatableCell="projectName" let-task>
            <span class="text-muted block truncate text-xs">
              {{ task.projectName }}
            </span>
          </ng-template>

          <ng-template appDatatableCell="assignees" let-task>
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
          </ng-template>

          <ng-template appDatatableCell="assign" let-task>
            @if (sprints().length > 0) {
              <app-dropdown-button
                #assignMenu
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
              <span class="text-muted text-sm">No sprints available</span>
            }
          </ng-template>
        </app-datatable>
      </div>
    </div>
  `,
})
export class SprintBacklogGroupComponent {
  private store = inject(Store);
  private hub = inject(ProjectTasksHubService);

  readonly label = input.required<string>();
  readonly categories = input.required<number[]>();
  readonly filterParams = input.required<Params>();
  readonly sprints = input.required<SprintViewModel[]>();

  readonly loading = this.store.selectSignal(selectSprintUpdateLoading);

  // Total backlog tasks for this group and whether its fetch has resolved,
  // pushed up from the datatable's own paginated fetch via its (loaded) output.
  private totalCount = signal(0);
  private resolved = signal(false);
  readonly count = this.totalCount.asReadonly();
  readonly hasLoaded = this.resolved.asReadonly();
  // Hide the whole group once we know it has no tasks. The datatable stays
  // mounted (hidden, not removed) so it keeps refetching when filters change.
  readonly isEmpty = computed(() => this.resolved() && this.totalCount() === 0);

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    this.totalCount.set(event.totalCount);
    this.resolved.set(event.hasValue);
  }

  private params = computed<Params>(() => ({
    statusCategories: this.categories(),
    ...this.filterParams(),
  }));

  readonly data: DatatableDataSource<TaskViewModel> = {
    key: 'sprint-backlog',
    columns: [
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-0',
      },
      { id: 'status', header: 'Status', sortable: true, widthClass: 'w-32' },
      {
        id: 'priority',
        header: 'Priority',
        sortable: true,
        widthClass: 'w-20',
      },
      {
        id: 'projectName',
        header: 'Project',
        sortable: true,
        widthClass: 'w-32',
      },
      {
        id: 'assignees',
        header: 'Assignees',
        sortable: true,
        widthClass: 'w-28',
      },
      { id: 'assign', header: 'Assign', widthClass: 'w-58' },
    ],
    resource: {
      url: 'api/sprints/backlog',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
    reloadSignal: this.hub.updateVersion,
  };

  onAssign(task: TaskViewModel, sprintId: number) {
    if (!task.id || !sprintId) return;

    this.store.dispatch(assignBacklogTask({ taskId: task.id, sprintId }));
  }
}
