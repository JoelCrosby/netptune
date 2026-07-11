import { httpResource } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentUserId } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { LucideCalendarClock } from '@lucide/angular';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SprintStatsComponent } from '@app/features/sprints/components/sprint-stats.component';
import { SprintStatusClassesPipe } from '@app/features/sprints/pipes/sprint-status-classes.pipe';
import { SprintStatusLabelPipe } from '@app/features/sprints/pipes/sprint-status-label.pipe';
import { sprintDaysChip } from '@app/features/sprints/utils/sprint-days-chip';
import { taskStatusBadgeClass } from '../utils/task-status-badge-class';

@Component({
  selector: 'app-dashboard-current-sprint-card',
  imports: [
    DatePipe,
    RouterLink,
    SpinnerComponent,
    TaskScopeIdComponent,
    SprintStatsComponent,
    SprintStatusClassesPipe,
    SprintStatusLabelPipe,
    LucideCalendarClock,
  ],
  template: `
    @if (isInitialLoad()) {
      <div
        class="border-border bg-card flex min-h-40 items-center justify-center rounded border p-6 shadow-sm">
        <app-spinner diameter="24" />
      </div>
    } @else if (sprint(); as sprint) {
      <section
        class="border-border bg-card flex flex-col gap-5 rounded border p-6 shadow-sm">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div class="min-w-0 flex-1">
            <p
              class="text-muted mb-1 flex items-center gap-1.5 text-xs font-semibold tracking-wide uppercase">
              <svg lucideCalendarClock class="h-3.5 w-3.5"></svg>
              Current sprint
            </p>
            <div class="mb-1 flex flex-wrap items-center gap-2">
              <a
                class="text-foreground truncate text-lg font-semibold hover:underline"
                [routerLink]="['../sprints', sprint.id]">
                {{ sprint.name }}
              </a>
              <span
                class="rounded-sm px-2 py-0.5 text-xs font-semibold"
                [class]="sprint.status | sprintStatusClasses">
                {{ sprint.status | sprintStatusLabel }}
              </span>
              @if (daysChip(); as chip) {
                <span
                  class="rounded-sm px-2 py-0.5 text-xs font-medium"
                  [class]="chip.classes">
                  {{ chip.label }}
                </span>
              }
            </div>
            <p class="text-muted text-sm">
              <span class="font-medium">{{ sprint.projectName }}</span>
              &nbsp;·&nbsp;
              {{ sprint.startDate | date: 'mediumDate' }} –
              {{ sprint.endDate | date: 'mediumDate' }}
            </p>
          </div>

          <a
            class="text-primary shrink-0 text-sm font-medium hover:underline"
            [routerLink]="['../sprints', sprint.id]">
            View sprint
          </a>
        </div>

        @if (sprint.goal) {
          <p class="text-muted text-sm">{{ sprint.goal }}</p>
        }

        <app-sprint-stats [sprint]="sprint" />

        <div class="flex flex-col gap-2">
          <h3
            class="text-foreground flex items-center gap-2 text-sm font-semibold">
            My tasks
            <span class="text-muted text-xs font-normal">{{
              myTasks().length
            }}</span>
          </h3>

          @if (myTasks().length > 0) {
            <ul class="divide-border/50 flex flex-col divide-y">
              @for (task of myTasks(); track task.id) {
                <li class="flex items-center gap-3 py-2">
                  <app-task-scope-id [id]="task.systemId" />
                  <a
                    class="min-w-0 flex-1 truncate text-sm font-medium hover:underline"
                    [routerLink]="['../tasks', task.systemId]">
                    {{ task.name }}
                  </a>
                  <span
                    class="inline-flex shrink-0 items-center rounded px-2 py-0.5 text-xs font-medium"
                    [class]="statusBadgeClass(task.statusCategory)">
                    {{ task.statusName }}
                  </span>
                </li>
              }
            </ul>
          } @else {
            <p class="text-muted text-sm">
              You have no tasks assigned to you in this sprint.
            </p>
          }
        </div>
      </section>
    }
  `,
})
export class DashboardCurrentSprintCardComponent {
  private readonly store = inject(Store);

  private readonly currentUserId = this.store.selectSignal(selectCurrentUserId);

  private readonly resource = httpResource<
    ClientResponse<SprintDetailViewModel | null>
  >(() => 'api/sprints/current');

  readonly sprint = computed(() => this.resource.value()?.payload ?? null);

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  readonly myTasks = computed<TaskViewModel[]>(() => {
    const userId = this.currentUserId();
    const tasks = this.sprint()?.tasks ?? [];

    if (!userId) return tasks;

    return tasks.filter((task) =>
      task.assignees.some((assignee) => assignee.id === userId)
    );
  });

  readonly daysChip = computed(() => {
    const sprint = this.sprint();
    return sprint ? sprintDaysChip(sprint.status, sprint.endDate) : null;
  });

  readonly statusBadgeClass = taskStatusBadgeClass;
}
