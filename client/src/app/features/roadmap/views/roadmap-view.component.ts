import { Component, computed, inject, viewChild } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { DialogService } from '@core/services/dialog.service';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { projectSprintRoute } from '@core/router/project-sprint-route';
import {
  queryParamSignal,
  queryParamsRoute,
} from '@core/router/query-param-signal';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { reloadToken } from '@core/util/signals';
import { appendTaskFilters } from '@core/util/task-filter-query';
import { TaskViewFiltersComponent } from '@shared/components/task-view-filters/task-view-filters.component';
import { delayedLoading } from '@core/util/delayed-loading';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SkeletonTimelineComponent } from '@static/components/skeleton/skeleton-timeline.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import {
  addDays,
  inclusiveDayCount,
  todayDate,
} from '@static/components/timeline/timeline-date-geometry';
import { TimelineZoom } from '@static/components/timeline/timeline.models';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { RoadmapFiltersComponent } from '../components/roadmap-filters.component';
import { RoadmapPlanningTimelineComponent } from '../components/roadmap-planning-timeline.component';
import { RoadmapUnscheduledComponent } from '../components/roadmap-unscheduled.component';
import { RoadmapScheduleChange, RoadmapTask } from '../models/roadmap.models';
import { roadmapResource } from '../resources/roadmap.resource';
import { validateRoadmapRange } from '../utils/roadmap-range';

const today = todayDate();
const defaultFrom = addDays(today, -45);
const defaultTo = addDays(today, 45);

@Component({
  selector: 'app-roadmap-view',
  imports: [
    ErrorStateComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    RoadmapFiltersComponent,
    SkeletonTimelineComponent,
    RoadmapPlanningTimelineComponent,
    RoadmapUnscheduledComponent,
    TaskViewFiltersComponent,
  ],
  template: `
    <app-page-container
      layout="list"
      [centerPage]="false"
      [showProgress]="roadmap.isLoading()">
      <app-page-header
        toolbar
        i18n-title="Page title for the roadmap"
        title="Roadmap" />

      <app-page-body scroll>
        <section
          class="border-border bg-card flex min-h-100 flex-1 flex-col overflow-hidden rounded-lg border">
          <app-roadmap-filters
            [from]="from()"
            [to]="to()"
            [zoom]="zoom()"
            [projectId]="projectId()"
            [projects]="projects()"
            [sprintId]="sprintId()"
            [sprints]="sprintOptions()"
            [includeUnscheduled]="includeUnscheduled()"
            (fromChanged)="from.set($event || null)"
            (toChanged)="to.set($event || null)"
            (zoomChanged)="zoom.set($event || null)"
            (projectChanged)="setProject($event)"
            (sprintChanged)="setSprint($event)"
            (includeUnscheduledChanged)="setUnscheduled($event)"
            (todayRequested)="showToday()"
            (rangeNavigationRequested)="navigateRange($event)"
            (refreshRequested)="refresh()" />

          <app-task-view-filters
            [search]="taskFilters().term ?? undefined"
            [assigneeIds]="taskFilters().users ?? []"
            [tagNames]="taskFilters().tags ?? []"
            [statusIds]="taskFilters().statuses ?? []"
            (searchChanged)="filterRoute.set('term', $event)"
            (assigneeIdsChanged)="filterRoute.set('users', $event)"
            (tagNamesChanged)="filterRoute.set('tags', $event)"
            (statusIdsChanged)="filterRoute.set('statusIds', $event)"
            (cleared)="filterRoute.clear()" />

          @if (rangeValidationError(); as validationError) {
            <div
              class="border-danger/30 bg-danger/5 text-danger m-4 rounded border p-4"
              role="alert">
              {{ validationError }}
            </div>
          } @else if (showSkeleton()) {
            <app-skeleton-timeline />
          } @else if (roadmap.error()) {
            <app-error-state
              compact
              i18n-title="Shown when the roadmap fails to load"
              title="The roadmap could not be loaded"
              i18n-description="Advice when the roadmap fails to load"
              description="Check the selected date range and try again."
              (retry)="roadmap.reload()" />
          } @else if (roadmap.value(); as view) {
            @if (view.truncated) {
              <div class="border-border border-b bg-amber-500/10 p-3 text-sm">
                <span
                  i18n="
                    Warning that the roadmap result was truncated. The 2,000
                    limit is fixed by the server
                  ">
                  This roadmap contains more than 2,000 scheduled tasks. Narrow
                  the project or date filters to see the complete result.
                </span>
              </div>
            }

            <app-roadmap-planning-timeline
              [view]="view"
              [from]="from()"
              [to]="to()"
              [zoom]="zoom()"
              [canUpdateTasks]="canUpdateTasks()"
              (refreshRequested)="refreshRoadmapData()"
              (taskSelected)="openTask($event)" />
          }
        </section>

        @if (roadmap.value()) {
          @if (includeUnscheduled()) {
            <app-roadmap-unscheduled
              [projectId]="projectId()"
              [sprintId]="sprintId()"
              [search]="taskFilters().term ?? undefined"
              [assigneeIds]="taskFilters().users ?? []"
              [tagNames]="taskFilters().tags ?? []"
              [statusIds]="taskFilters().statuses ?? []"
              [canUpdateTasks]="canUpdateTasks()"
              [scheduleDate]="from()"
              [reloadSignal]="unscheduledReload"
              (scheduleRequested)="scheduleTask($event)"
              (taskSelected)="openTask($event)" />
          }
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class RoadmapViewComponent {
  private readonly dialog = inject(DialogService);
  private readonly planningTimeline = viewChild(
    RoadmapPlanningTimelineComponent
  );

  private readonly queryParams = queryParamsRoute();
  private readonly projectSprint = projectSprintRoute();

  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;
  readonly sprintsResource = sprintResource([]);
  readonly sprints = this.sprintsResource.value;

  readonly canUpdateTasks = hasPermission(PERMISSIONS.tasks.update);
  readonly canReadSprints = hasPermission(PERMISSIONS.sprints.read);
  readonly unscheduledReload = reloadToken();
  readonly from = queryParamSignal('from', (value) => value ?? defaultFrom);
  readonly to = queryParamSignal('to', (value) => value ?? defaultTo);
  readonly projectId = this.projectSprint.projectId;
  readonly sprintId = this.projectSprint.sprintId;
  protected readonly filterRoute = taskFilterRoute();
  readonly taskFilters = this.filterRoute.filters;
  readonly includeUnscheduled = queryParamSignal(
    'unscheduled',
    (value) => value !== 'false'
  );
  readonly rangeValidationError = computed(() =>
    validateRoadmapRange(this.from(), this.to())
  );

  readonly zoom = queryParamSignal('zoom', (value): TimelineZoom => {
    return value === 'day' || value === 'month' ? value : 'week';
  });

  readonly query = computed<string | undefined>(() => {
    if (this.rangeValidationError()) {
      return undefined;
    }

    const query = new URLSearchParams({
      from: this.from(),
      to: this.to(),
    });
    const projectId = this.projectId();
    const sprintId = this.sprintId();
    const taskFilters = this.taskFilters();

    if (projectId) {
      query.set('projectIds', String(projectId));
    }

    if (sprintId) {
      query.set('sprintIds', String(sprintId));
    }

    appendTaskFilters(query, taskFilters);

    return query.toString();
  });

  readonly roadmap = roadmapResource(this.query);

  readonly showSkeleton = delayedLoading(
    computed(() => this.roadmap.isLoading() && !this.roadmap.hasValue())
  );

  readonly sprintOptions = computed(() =>
    this.sprints().length > 0
      ? this.sprints()
      : (this.roadmap.value()?.sprints ?? [])
  );

  constructor() {
    this.ensureDefaultParams();
  }

  setProject(projectId: number | null): void {
    this.projectSprint.setProject(projectId);
  }

  setSprint(sprintId: number | null): void {
    const sprint = this.sprintOptions().find((item) => item.id === sprintId);
    this.projectSprint.setSprint(sprintId, sprint?.projectId);
  }

  setUnscheduled(includeUnscheduled: boolean): void {
    this.includeUnscheduled.set(String(includeUnscheduled));
  }

  showToday(): void {
    const centre = todayDate();
    this.queryParams.patch({
      from: addDays(centre, -45),
      to: addDays(centre, 45),
    });
  }

  navigateRange(direction: -1 | 1): void {
    const rangeDays = Math.max(1, inclusiveDayCount(this.from(), this.to()));
    const offset = rangeDays * direction;
    this.queryParams.patch({
      from: addDays(this.from(), offset),
      to: addDays(this.to(), offset),
    });
  }

  refresh(): void {
    const planningTimeline = this.planningTimeline();

    if (planningTimeline) {
      planningTimeline.requestRefresh();
    } else {
      this.refreshRoadmap();
    }

    this.unscheduledReload.bump();
  }

  refreshRoadmap(): void {
    this.roadmap.reload();
  }

  refreshRoadmapData(): void {
    this.refreshRoadmap();
    this.unscheduledReload.bump();
  }

  scheduleTask(change: RoadmapScheduleChange): void {
    this.planningTimeline()?.updateSchedule(change);
  }

  async openTask(task: RoadmapTask): Promise<void> {
    await this.dialog.openForResult(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      height: TaskDetailDialogComponent.height,
      data: task,
      autoFocus: false,
      panelClass: TaskDetailDialogComponent.panelClass,
    });

    this.refresh();
  }

  private ensureDefaultParams(): void {
    const query = this.queryParams.paramMap();
    const hasDefaults =
      query.has('from') && query.has('to') && query.has('zoom');

    if (hasDefaults) {
      return;
    }

    this.queryParams.patch(
      {
        from: query.get('from') ?? defaultFrom,
        to: query.get('to') ?? defaultTo,
        zoom: query.get('zoom') ?? 'week',
      },
      { replaceUrl: true }
    );
  }
}
