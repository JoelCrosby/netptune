import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { DialogService } from '@core/services/dialog.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { loadSprints } from '@core/store/sprints/sprints.actions';
import { selectAllSprints } from '@core/store/sprints/sprints.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
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
import { RoadmapTask } from '../models/roadmap.models';
import { roadmapResource } from '../resources/roadmap.resource';

const today = todayDate();
const defaultFrom = addDays(today, -45);
const defaultTo = addDays(today, 45);

@Component({
  selector: 'app-roadmap-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    RoadmapFiltersComponent,
    RoadmapPlanningTimelineComponent,
    RoadmapUnscheduledComponent,
  ],
  template: `
    <app-page-container
      [centerPage]="false"
      [fullHeight]="true"
      [showProgress]="roadmap.isLoading()">
      <app-page-header title="Roadmap" />

      <section
        class="border-border bg-card flex h-[calc(100vh-180px)] min-h-0 flex-none flex-col overflow-hidden rounded-lg border max-[600px]:h-[calc(100vh-154px)]">
        <app-roadmap-filters
          [from]="from()"
          [to]="to()"
          [zoom]="zoom()"
          [projectId]="projectId()"
          [projects]="projects()"
          [sprintId]="sprintId()"
          [sprints]="sprintOptions()"
          [includeUnscheduled]="includeUnscheduled()"
          (fromChanged)="setParam('from', $event)"
          (toChanged)="setParam('to', $event)"
          (zoomChanged)="setParam('zoom', $event)"
          (projectChanged)="setProject($event)"
          (sprintChanged)="setSprint($event)"
          (includeUnscheduledChanged)="setUnscheduled($event)"
          (todayRequested)="showToday()"
          (rangeNavigationRequested)="navigateRange($event)"
          (refreshRequested)="refresh()" />

        @if (roadmap.error()) {
          <div
            class="border-danger/30 bg-danger/5 text-danger m-4 rounded border p-4">
            The roadmap could not be loaded. Check the selected date range and
            try again.
          </div>
        } @else if (roadmap.value(); as view) {
          @if (view.truncated) {
            <div class="border-border border-b bg-amber-500/10 p-3 text-sm">
              This roadmap contains more than 2,000 scheduled tasks. Narrow the
              project or date filters to see the complete result.
            </div>
          }

          <app-roadmap-planning-timeline
            [view]="view"
            [from]="from()"
            [to]="to()"
            [zoom]="zoom()"
            [canUpdateTasks]="canUpdateTasks()"
            [realtimeGroup]="realtimeGroup()"
            (refreshRequested)="refreshRoadmap()"
            (taskSelected)="openTask($event)" />
        }
      </section>

      @if (roadmap.value()) {
        @if (includeUnscheduled()) {
          <app-roadmap-unscheduled
            [projectId]="projectId()"
            [sprintId]="sprintId()"
            [reloadSignal]="unscheduledReload"
            (taskSelected)="openTask($event)" />
        }
      }
    </app-page-container>
  `,
})
export class RoadmapViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly dialog = inject(DialogService);
  private readonly planningTimeline = viewChild(
    RoadmapPlanningTimelineComponent
  );

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
  readonly unscheduledReload = signal(0);
  readonly from = computed(() => this.params().get('from') ?? defaultFrom);
  readonly to = computed(() => this.params().get('to') ?? defaultTo);
  readonly projectId = computed(() => this.numberParam('projectId'));
  readonly sprintId = computed(() => this.numberParam('sprintId'));
  readonly includeUnscheduled = computed(
    () => this.params().get('unscheduled') !== 'false'
  );

  readonly zoom = computed<TimelineZoom>(() => {
    const value = this.params().get('zoom');
    return value === 'day' || value === 'month' ? value : 'week';
  });

  readonly query = computed(() => {
    const query = new URLSearchParams({
      from: this.from(),
      to: this.to(),
    });
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

  readonly roadmap = roadmapResource(this.query);
  readonly realtimeGroup = computed(() => {
    const workspace = this.workspaceIdentifier();
    return workspace ? `tasks:${workspace}` : undefined;
  });

  readonly sprintOptions = computed(() =>
    this.sprints().length > 0
      ? this.sprints()
      : (this.roadmap.value()?.sprints ?? [])
  );

  constructor() {
    this.store.dispatch(loadProjects.init());

    if (this.canReadSprints()) {
      this.store.dispatch(loadSprints.init({ filter: { take: 100 } }));
    }

    this.ensureDefaultParams();
  }

  setParam(key: string, value: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [key]: value || null },
      queryParamsHandling: 'merge',
    });
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
    const sprint = this.sprintOptions().find((item) => item.id === sprintId);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sprintId,
        projectId: sprint?.projectId ?? this.projectId() ?? null,
      },
      queryParamsHandling: 'merge',
    });
  }

  setUnscheduled(includeUnscheduled: boolean): void {
    this.setParam('unscheduled', String(includeUnscheduled));
  }

  showToday(): void {
    const centre = todayDate();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        from: addDays(centre, -45),
        to: addDays(centre, 45),
      },
      queryParamsHandling: 'merge',
    });
  }

  navigateRange(direction: -1 | 1): void {
    const rangeDays = Math.max(1, inclusiveDayCount(this.from(), this.to()));
    const offset = rangeDays * direction;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        from: addDays(this.from(), offset),
        to: addDays(this.to(), offset),
      },
      queryParamsHandling: 'merge',
    });
  }

  refresh(): void {
    const planningTimeline = this.planningTimeline();

    if (planningTimeline) {
      planningTimeline.requestRefresh();
    } else {
      this.refreshRoadmap();
    }

    this.unscheduledReload.update((value) => value + 1);
  }

  refreshRoadmap(): void {
    this.roadmap.reload();
  }

  openTask(task: RoadmapTask): void {
    const dialogRef = this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: task,
      autoFocus: false,
      panelClass: 'app-modal-class',
    });

    dialogRef.closed.subscribe(() => {
      this.refresh();
    });
  }

  private numberParam(key: string): number | undefined {
    const value = Number(this.params().get(key));
    return Number.isInteger(value) && value > 0 ? value : undefined;
  }

  private ensureDefaultParams(): void {
    const query = this.route.snapshot.queryParamMap;
    const hasDefaults =
      query.has('from') && query.has('to') && query.has('zoom');

    if (hasDefaults) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        from: query.get('from') ?? defaultFrom,
        to: query.get('to') ?? defaultTo,
        zoom: query.get('zoom') ?? 'week',
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
