import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import {
  completeSprint,
  addTasksToSprint,
  loadSprintDetail,
  removeTaskFromSprint,
  startSprint,
} from '@core/store/sprints/sprints.actions';
import {
  selectAvailableSprintTasks,
  selectSprintDetail,
  selectSprintDetailLoading,
  selectSprintUpdateLoading,
} from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { FormSelectSearchComponent } from '@static/components/form-select-search/form-select-search.component';
import { distinctUntilChanged, map } from 'rxjs/operators';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    CardComponent,
    TaskScopeIdComponent,
    FormSelectSearchComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Sprint" />

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else if (sprint(); as sprint) {
        <section class="flex flex-col gap-4">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 class="mb-2 text-2xl font-semibold">{{ sprint.name }}</h1>
              <p class="text-muted text-sm">
                <span class="font-semibold">{{ sprint.projectName }}</span> ·
                {{ sprint.startDate | date: 'mediumDate' }} -
                {{ sprint.endDate | date: 'mediumDate' }}
              </p>
            </div>

            <div class="flex items-center gap-8">
              <span
                class="rounded-sm bg-neutral-200 px-4 py-1 text-sm font-semibold text-neutral-800">
                {{ statusLabel(sprint.status) }}
              </span>

              @if (
                canUpdateSprints() && sprint.status === sprintStatus.planning
              ) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  [disabled]="updateLoading()"
                  (click)="onStart(sprint.id)">
                  Start
                </button>
              }

              @if (
                canUpdateSprints() && sprint.status === sprintStatus.active
              ) {
                <button
                  app-flat-button
                  color="primary"
                  type="button"
                  [disabled]="updateLoading()"
                  (click)="onComplete(sprint.id)">
                  Complete
                </button>
              }
            </div>
          </div>

          @if (sprint.goal) {
            <p class="mt-4">{{ sprint.goal }}</p>
          }

          <div class="bg-board-group p-2">
            <div class="grid gap-3 md:grid-cols-4">
              <app-card class="min-h-0! p-4!">
                <div class="text-muted mb-2 text-sm">Total</div>
                <div class="text-2xl font-semibold">{{ sprint.taskCount }}</div>
              </app-card>
              <app-card class="min-h-0! p-4!">
                <div class="text-muted mb-2 text-sm">New</div>
                <div class="text-2xl font-semibold">
                  {{ sprint.newTaskCount }}
                </div>
              </app-card>
              <app-card class="min-h-0! p-4!">
                <div class="text-muted mb-2 text-sm">In Progress</div>
                <div class="text-2xl font-semibold">
                  {{ sprint.activeTaskCount }}
                </div>
              </app-card>
              <app-card class="min-h-0! p-4!">
                <div class="text-muted mb-2 text-sm">Complete</div>
                <div class="text-2xl font-semibold">
                  {{ sprint.doneTaskCount }}
                </div>
              </app-card>
            </div>
          </div>

          <div class="bg-board-group p-2">
            <app-card class="min-h-0! p-0!">
              @if (
                canManageSprintTasks() &&
                sprint.status !== sprintStatus.completed
              ) {
                <form
                  class="border-border flex flex-wrap items-center gap-3 border-b p-4"
                  (submit)="onAddTask($event, sprint.id)">
                  <app-form-select-search
                    class="min-w-64 flex-1"
                    label="Add Task"
                    placeholder="Select a task"
                    emptyMessage="No matching tasks"
                    [options]="availableTasks()"
                    [labelWith]="taskSelectLabel"
                    [valueWith]="taskSelectValue"
                    [value]="selectedTaskId ?? null"
                    (changed)="onTaskSelected($event)">
                  </app-form-select-search>

                  <button
                    app-flat-button
                    color="primary"
                    type="submit"
                    [disabled]="updateLoading() || !selectedTaskId">
                    Add
                  </button>
                </form>
              }

              @for (task of sprint.tasks; track task.id) {
                <div
                  class="border-border flex items-center justify-between gap-4 border-b p-4 last:border-b-0">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <app-task-scope-id [id]="task.systemId" />
                      <a
                        class="truncate font-medium"
                        [routerLink]="['../../tasks', task.systemId]">
                        {{ task.name }}
                      </a>
                    </div>
                    <div class="text-muted mt-1 text-sm">
                      {{ task.projectName }}
                    </div>
                  </div>

                  @if (
                    canManageSprintTasks() &&
                    sprint.status !== sprintStatus.completed
                  ) {
                    <button
                      app-stroked-button
                      color="primary"
                      type="button"
                      [disabled]="updateLoading()"
                      (click)="onRemoveTask(sprint.id, task.id)">
                      Remove
                    </button>
                  }
                </div>
              } @empty {
                <div class="p-6 text-center">No tasks in this sprint.</div>
              }
            </app-card>
          </div>
        </section>
      }
    </app-page-container>
  `,
})
export class SprintDetailViewComponent {
  private store = inject(Store);
  private route = inject(ActivatedRoute);

  readonly sprintStatus = SprintStatus;
  readonly sprint = this.store.selectSignal(selectSprintDetail);
  readonly loading = this.store.selectSignal(selectSprintDetailLoading);
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);
  readonly availableTasks = this.store.selectSignal(selectAvailableSprintTasks);
  readonly canUpdateSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.update)
  );
  readonly canManageSprintTasks = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.manageTasks)
  );

  selectedTaskId?: number;
  taskSelectLabel = (task: TaskViewModel) => `${task.systemId} · ${task.name}`;
  taskSelectValue = (task: TaskViewModel) => task.id;

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
        takeUntilDestroyed()
      )
      .subscribe((sprintId) => {
        if (Number.isFinite(sprintId) && sprintId > 0) {
          this.selectedTaskId = undefined;
          this.store.dispatch(loadSprintDetail({ sprintId }));
        }
      });

    effect(() => {
      const selectedTaskId = this.selectedTaskId;

      if (
        selectedTaskId &&
        !this.availableTasks().some((task) => task.id === selectedTaskId)
      ) {
        this.selectedTaskId = undefined;
      }
    });
  }

  statusLabel(status: SprintStatus) {
    return sprintStatusLabels[status];
  }

  onStart(sprintId?: number) {
    if (!sprintId) return;

    this.store.dispatch(startSprint({ sprintId }));
  }

  onComplete(sprintId?: number) {
    if (!sprintId) return;

    this.store.dispatch(completeSprint({ sprintId }));
  }

  onRemoveTask(sprintId?: number, taskId?: number) {
    if (!sprintId || !taskId) return;

    this.store.dispatch(removeTaskFromSprint({ sprintId, taskId }));
  }

  onTaskSelected(taskId: number) {
    this.selectedTaskId = taskId;
  }

  onAddTask(event: Event, sprintId?: number) {
    event.preventDefault();

    if (!sprintId || !this.selectedTaskId) return;

    this.store.dispatch(
      addTasksToSprint({
        sprintId,
        request: { taskIds: [this.selectedTaskId] },
      })
    );

    this.selectedTaskId = undefined;
  }
}
