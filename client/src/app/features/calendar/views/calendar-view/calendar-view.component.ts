import {
  Component,
  computed,
  inject,
  linkedSignal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ScheduledTask } from '@core/models/scheduled-task';
import { DialogService } from '@core/services/dialog.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { loadSprints } from '@core/store/sprints/sprints.actions';
import { selectAllSprints } from '@core/store/sprints/sprints.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { todayDate } from '@static/components/timeline/timeline-date-geometry';
import { CalendarPlanningMonthComponent } from '../../components/calendar-planning-month/calendar-planning-month.component';
import { CalendarToolbarComponent } from '../../components/calendar-toolbar/calendar-toolbar.component';
import { calendarResource } from '../../resources/calendar.resource';
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
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container
      [centerPage]="false"
      [fullHeight]="true"
      [showProgress]="calendar.isLoading()">
      <app-page-header title="Calendar" />

      <section
        class="border-border bg-card flex h-[calc(100vh-180px)] min-h-0 flex-none flex-col overflow-hidden rounded-lg border max-[600px]:h-[calc(100vh-154px)]">
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

        @if (calendar.error()) {
          <div
            class="border-danger/30 bg-danger/5 text-danger m-4 rounded border p-4"
            role="alert">
            The calendar could not be loaded. Try refreshing the view.
          </div>
        } @else if (calendar.value(); as view) {
          @if (view.truncated) {
            <div class="border-border border-b bg-amber-500/10 p-3 text-sm">
              This calendar contains more than 2,000 scheduled tasks. Narrow the
              project or sprint filter to see the complete result.
            </div>
          }

          <app-calendar-planning-month
            [view]="view"
            [days]="range().days"
            [(selectedDate)]="selectedDate"
            [canUpdateTasks]="canUpdateTasks()"
            [projectId]="projectId()"
            [sprintId]="sprintId()"
            [realtimeGroup]="realtimeGroup()"
            (refreshRequested)="refreshCalendar()"
            (taskSelected)="openTask($event)" />
        }
      </section>
    </app-page-container>
  `,
  styles: ``,
})
export class CalendarViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly dialog = inject(DialogService);
  private readonly planningMonth = viewChild(CalendarPlanningMonthComponent);

  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  readonly projects = this.store.selectSignal(selectAllProjects);
  readonly sprints = this.store.selectSignal(selectAllSprints);
  readonly workspaceIdentifier = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );
  readonly canUpdateTasks = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.update)
  );
  readonly canReadSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );

  readonly month = computed(() =>
    validCalendarMonth(this.params().get('month'))
  );
  readonly range = computed(() => calendarMonthRange(this.month()));
  readonly projectId = computed(() => this.numberParam('projectId'));
  readonly sprintId = computed(() => this.numberParam('sprintId'));
  readonly query = computed(() => {
    const range = this.range();
    const query = new URLSearchParams({ from: range.from, to: range.to });
    const projectId = this.projectId();
    const sprintId = this.sprintId();

    if (projectId) {
      query.set('projectIds', String(projectId));
    }
    if (sprintId) {
      query.set('sprintIds', String(sprintId));
    }

    return query.toString();
  });
  readonly calendar = calendarResource(this.query);
  readonly realtimeGroup = computed(() => {
    const workspace = this.workspaceIdentifier();
    return workspace ? `tasks:${workspace}` : undefined;
  });
  readonly selectedDate = linkedSignal(() => {
    const range = this.range();
    const today = todayDate();
    return today >= range.from && today <= range.to
      ? today
      : `${range.month}-01`;
  });

  constructor() {
    this.store.dispatch(loadProjects.init());
    if (this.canReadSprints()) {
      this.store.dispatch(loadSprints.init({ filter: { take: 100 } }));
    }

    this.ensureDefaultMonth();
  }

  navigateMonth(direction: -1 | 1): void {
    this.setParam('month', addCalendarMonths(this.month(), direction));
  }

  showToday(): void {
    this.setParam('month', todayDate().slice(0, 7));
  }

  setProject(projectId: number | null): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        projectId: projectId?.toString() ?? null,
        sprintId: null,
      },
      queryParamsHandling: 'merge',
    });
  }

  setSprint(sprintId: number | null): void {
    const sprint = this.sprints().find((item) => item.id === sprintId);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sprintId,
        projectId: sprint?.projectId ?? this.projectId() ?? null,
      },
      queryParamsHandling: 'merge',
    });
  }

  refresh(): void {
    const planningMonth = this.planningMonth();
    if (planningMonth) {
      planningMonth.requestRefresh();
    } else {
      this.refreshCalendar();
    }
  }

  openTask(task: ScheduledTask): void {
    const dialogRef = this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: task,
      autoFocus: false,
      panelClass: 'app-modal-class',
    });
    dialogRef.closed.subscribe(() => this.refresh());
  }

  private setParam(key: string, value: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [key]: value },
      queryParamsHandling: 'merge',
    });
  }

  refreshCalendar(): void {
    this.calendar.reload();
  }

  private numberParam(key: string): number | undefined {
    const value = Number(this.params().get(key));
    return Number.isInteger(value) && value > 0 ? value : undefined;
  }

  private ensureDefaultMonth(): void {
    if (this.route.snapshot.queryParamMap.has('month')) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { month: this.month() },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
