import {
  Component,
  computed,
  inject,
  linkedSignal,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ScheduledTask } from '@core/models/scheduled-task';
import { DialogService } from '@core/services/dialog.service';
import { projectSprintRoute } from '@core/router/project-sprint-route';
import {
  queryParamSignal,
  queryParamsRoute,
} from '@core/router/query-param-signal';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { appendTaskFilters } from '@core/util/task-filter-query';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { delayedLoading } from '@core/util/delayed-loading';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SkeletonCalendarMonthComponent } from '@static/components/skeleton/skeleton-calendar-month.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { TaskViewFiltersComponent } from '@shared/components/task-view-filters/task-view-filters.component';
import { todayDate } from '@static/components/timeline/timeline-date-geometry';
import { CalendarPlanningMonthComponent } from '../../components/calendar-planning-month/calendar-planning-month.component';
import { CalendarToolbarComponent } from '../../components/calendar-toolbar/calendar-toolbar.component';
import { calendarResource } from '../../resources/calendar.resource';
import { PanelComponent } from '@static/components/panel.component';
import {
  addCalendarMonths,
  calendarMonthRange,
  validCalendarMonth,
} from '../../utils/calendar-range';

@Component({
  selector: 'app-calendar-view',
  imports: [
    CalendarPlanningMonthComponent,
    CalendarToolbarComponent,
    ErrorStateComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    SkeletonCalendarMonthComponent,
    TaskViewFiltersComponent,
  ],
  template: `
    <app-page-container
      layout="list"
      [centerPage]="false"
      [showProgress]="calendar.isLoading()">
      <app-page-header
        toolbar
        i18n-title="Page title for the calendar"
        title="Calendar" />

      <app-page-body>
        <section app-panel surface="card" class="flex min-h-0 flex-1 flex-col">
          <app-calendar-toolbar
            [monthLabel]="range().label"
            [projectId]="projectId()"
            [projects]="projects()"
            [sprintId]="sprintId()"
            [sprints]="sprints()"
            (projectChanged)="setProject($event)"
            (sprintChanged)="setSprint($event)"
            (monthNavigationRequested)="navigateMonth($event)"
            (todayRequested)="showToday()"
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

          @if (showSkeleton()) {
            <app-skeleton-calendar-month />
          } @else if (calendar.error()) {
            <app-error-state
              compact
              i18n-title="Shown when the calendar fails to load"
              title="The calendar could not be loaded"
              i18n-description="Advice when the calendar fails to load"
              description="Check the selected filters and try again."
              (retry)="calendar.reload()" />
          } @else if (calendar.value(); as view) {
            @if (view.truncated) {
              <div class="border-border border-b bg-amber-500/10 p-3 text-sm">
                <span
                  i18n="
                    Warning that the calendar result was truncated. The 2,000
                    limit is fixed by the server
                  ">
                  This calendar contains more than 2,000 scheduled tasks. Narrow
                  the project or sprint filter to see the complete result.
                </span>
              </div>
            }

            <app-calendar-planning-month
              [view]="view"
              [days]="range().days"
              [(selectedDate)]="selectedDate"
              [canUpdateTasks]="canUpdateTasks()"
              [projectId]="projectId()"
              [sprintId]="sprintId()"
              [search]="taskFilters().term ?? undefined"
              [assigneeIds]="taskFilters().users ?? []"
              [tagNames]="taskFilters().tags ?? []"
              [statusIds]="taskFilters().statuses ?? []"
              (refreshRequested)="refreshCalendar()"
              (taskSelected)="openTask($event)" />
          }
        </section>
      </app-page-body>
    </app-page-container>
  `,
  styles: ``,
})
export class CalendarViewComponent {
  private readonly dialog = inject(DialogService);
  private readonly planningMonth = viewChild(CalendarPlanningMonthComponent);

  private readonly queryParams = queryParamsRoute();
  private readonly projectSprint = projectSprintRoute();

  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;
  readonly sprintsResource = sprintResource([]);
  readonly sprints = this.sprintsResource.value;
  readonly canUpdateTasks = hasPermission(PERMISSIONS.tasks.update);
  readonly canReadSprints = hasPermission(PERMISSIONS.sprints.read);

  readonly month = queryParamSignal('month', validCalendarMonth);
  readonly range = computed(() => calendarMonthRange(this.month()));
  readonly projectId = this.projectSprint.projectId;
  readonly sprintId = this.projectSprint.sprintId;
  protected readonly filterRoute = taskFilterRoute();
  readonly taskFilters = this.filterRoute.filters;
  readonly query = computed(() => {
    const range = this.range();
    const query = new URLSearchParams({ from: range.from, to: range.to });
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
  readonly calendar = calendarResource(this.query);

  readonly showSkeleton = delayedLoading(
    computed(() => this.calendar.isLoading() && !this.calendar.hasValue())
  );
  readonly selectedDate = linkedSignal(() => {
    const range = this.range();
    const today = todayDate();
    return today >= range.from && today <= range.to
      ? today
      : `${range.month}-01`;
  });

  constructor() {
    this.ensureDefaultMonth();
  }

  navigateMonth(direction: -1 | 1): void {
    this.month.set(addCalendarMonths(this.month(), direction));
  }

  showToday(): void {
    this.month.set(todayDate().slice(0, 7));
  }

  setProject(projectId: number | null): void {
    this.projectSprint.setProject(projectId);
  }

  setSprint(sprintId: number | null): void {
    const sprint = this.sprints().find((item) => item.id === sprintId);
    this.projectSprint.setSprint(sprintId, sprint?.projectId);
  }

  refresh(): void {
    const planningMonth = this.planningMonth();
    if (planningMonth) {
      planningMonth.requestRefresh();
    } else {
      this.refreshCalendar();
    }
  }

  async openTask(task: ScheduledTask): Promise<void> {
    await this.dialog.openForResult(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      height: TaskDetailDialogComponent.height,
      data: task,
      autoFocus: false,
      panelClass: TaskDetailDialogComponent.panelClass,
    });

    this.refresh();
  }

  refreshCalendar(): void {
    this.calendar.reload();
  }

  private ensureDefaultMonth(): void {
    const hasMonth = this.queryParams.paramMap().has('month');

    if (hasMonth) {
      return;
    }

    this.month.set(this.month(), { replaceUrl: true });
  }
}
