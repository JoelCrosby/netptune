import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EstimateType, estimateTypeUnits } from '@core/enums/estimate-type';
import { ClientResponse } from '@core/models/client-response';
import { SprintBurndownReport } from '@core/models/reporting';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { hostTimeZone } from '@core/util/dates';
import { LucideCalendarClock, LucideCalendarOff } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';
import { SprintStatusBadgeComponent } from '@static/components/sprint-status-badge.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { SprintBurndownSparklineComponent } from './sprint-burndown-sparkline.component';

@Component({
  selector: 'app-dashboard-current-sprint-card',
  imports: [
    DatePipe,
    EmptyStateComponent,
    IconTileComponent,
    LucideCalendarOff,
    ProgressBarComponent,
    RouterLink,
    SkeletonComponent,
    SprintBurndownSparklineComponent,
    SprintDaysBadgeComponent,
    SprintStatusBadgeComponent,
    StatStripComponent,
  ],
  template: `
    @if (isInitialLoad()) {
      <section
        class="border-border bg-card rounded-lg border p-6 shadow-sm"
        role="status"
        i18n-aria-label="Accessible label while the current sprint loads"
        aria-label="Loading current sprint">
        <div class="flex items-start gap-3">
          <app-skeleton class="h-9 w-9 rounded-lg" />
          <div class="flex-1">
            <app-skeleton class="h-3 w-28" />
            <app-skeleton class="mt-2 h-5 w-56" />
          </div>
        </div>
        <app-skeleton class="mt-6 h-8 w-32" />
        <app-skeleton class="mt-4 h-2 w-full rounded-full" />
        <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
          @for (tile of skeletonTiles; track $index) {
            <app-skeleton class="h-10" />
          }
        </div>
      </section>
    } @else if (sprint(); as sprint) {
      <section
        class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
        <header
          class="border-border flex flex-wrap items-start justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
          <div class="flex min-w-0 items-start gap-3">
            <app-icon-tile [icon]="sprintIcon" />

            <div class="min-w-0">
              <p
                class="text-muted text-xs font-semibold tracking-wide uppercase"
                i18n="Heading of the dashboard current-sprint card">
                Current sprint
              </p>

              <div class="mt-1 flex flex-wrap items-center gap-2">
                <a
                  class="font-overpass text-foreground truncate text-lg font-semibold hover:underline"
                  [routerLink]="['../sprints', sprint.id]">
                  {{ sprint.name }}
                </a>
                <app-sprint-status-badge [status]="sprint.status" />
                <app-sprint-days-badge
                  [status]="sprint.status"
                  [endDate]="sprint.endDate" />
              </div>

              <p class="text-muted mt-1 text-sm">
                <span class="font-medium">{{ sprint.projectName }}</span>
                &nbsp;·&nbsp;
                {{ sprint.startDate | date: 'mediumDate' }} –
                {{ sprint.endDate | date: 'mediumDate' }}
              </p>
            </div>
          </div>

          <a
            class="text-primary shrink-0 text-sm font-medium hover:underline"
            [routerLink]="['../sprints', sprint.id]">
            <span i18n="Link to the current sprint">View sprint</span>
          </a>
        </header>

        <div class="px-6 py-5">
          @if (sprint.taskCount > 0) {
            <div class="flex flex-wrap items-baseline justify-between gap-x-4">
              <p
                class="flex items-baseline gap-2"
                i18n="Sprint completion. PERCENT is a whole number">
                <span
                  class="text-3xl font-semibold tracking-tight tabular-nums">
                  {{
                    progressPercent()  // i18n(ph="PERCENT")
                  }}%
                </span>
                complete
              </p>

              <p
                class="text-muted text-sm tabular-nums"
                i18n="
                  Sprint task progress. DONE is finished tasks and TOTAL the
                  total
                ">
                {{
                  sprint.doneTaskCount // i18n(ph="DONE")
                }}
                /
                {{
                  sprint.taskCount // i18n(ph="TOTAL")
                }}
                tasks
              </p>
            </div>

            <app-progress-bar class="mt-4 h-2" [value]="progressPercent()" />
          }

          @if (sprint.goal) {
            <p class="text-muted text-sm" [class.mt-4]="sprint.taskCount > 0">
              {{ sprint.goal }}
            </p>
          }
        </div>

        <app-stat-strip [items]="stats()" />

        @if (burndownPoints().length > 1) {
          <div class="border-border border-t px-6 py-5">
            <app-sprint-burndown-sparkline [points]="burndownPoints()" />
            <a
              class="text-primary mt-2 block text-right text-xs font-medium hover:underline"
              [routerLink]="['../reports']"
              [queryParams]="{ sprintId: sprint.id }">
              <span i18n="Link to the sprint burndown report">
                View report →
              </span>
            </a>
          </div>
        }
      </section>
    } @else {
      <section class="border-border bg-card rounded-lg border p-6 shadow-sm">
        <app-empty-state
          compact
          i18n-title="Empty state when no sprint is running"
          title="No active sprint."
          i18n-description="Advice shown when no sprint is running"
          description="Start a sprint to track progress here.">
          <svg emptyStateIcon lucideCalendarOff class="h-8 w-8"></svg>
        </app-empty-state>
      </section>
    }
  `,
})
export class DashboardCurrentSprintCardComponent {
  private readonly resource = httpResource<
    ClientResponse<SprintDetailViewModel | null>
  >(() => 'api/sprints/current');

  protected readonly sprintIcon = LucideCalendarClock;
  protected readonly skeletonTiles = Array.from({ length: 3 });

  readonly sprint = computed(() => this.resource.value()?.payload ?? null);

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  // Sprint report data. Fails quietly (pre-coverage sprint, no baseline, or the viewer lacks
  // reporting access) — the card still renders from the sprint stats above.
  private readonly burndown = httpResource<SprintBurndownReport>(() => {
    const sprint = this.sprint();
    return sprint
      ? `api/reports/sprints/${sprint.id}/burndown?unit=Tasks&timeZone=${encodeURIComponent(hostTimeZone())}`
      : undefined;
  });

  readonly burndownPoints = computed(() => this.burndown.value()?.points ?? []);

  readonly progressPercent = computed(() => {
    const sprint = this.sprint();
    if (!sprint?.taskCount) return 0;
    return Math.round((sprint.doneTaskCount / sprint.taskCount) * 100);
  });

  readonly stats = computed<StatStripItem[]>(() => {
    const sprint = this.sprint();
    if (!sprint) return [];

    const tiles: StatStripItem[] = [
      {
        label: $localize`:Label shown in the interface:Remaining`,
        value: sprint.taskCount - sprint.doneTaskCount,
      },
    ];

    const report = this.burndown.value();
    if (report) {
      tiles.push({
        label: $localize`:Label shown in the interface:Scope`,
        value: scopeLabel(report),
      });
    }

    const estimate = estimateStat(sprint);
    if (estimate) {
      tiles.push(estimate);
    }

    if (tiles.length < 2) {
      tiles.push({
        label: $localize`:Label shown in the interface:Completed`,
        value: sprint.doneTaskCount,
      });
    }

    return tiles;
  });
}

function scopeLabel(report: SprintBurndownReport): string {
  const net = report.addedCount - report.removedCount;
  if (net > 0) return `${report.committedCount} (+${net})`;
  if (net < 0) return `${report.committedCount} (${net})`;
  return `${report.committedCount}`;
}

function estimateStat(sprint: SprintDetailViewModel): StatStripItem | null {
  const type = sprint.estimateType;
  const value = sprint.totalEstimateValue;

  // Story points and hours are the only numeric estimate units; t-shirt sizes are categorical.
  const isNumericUnit =
    type === EstimateType.storyPoints || type === EstimateType.hours;

  if (!isNumericUnit || value == null) return null;

  return {
    label: $localize`:Label shown in the interface:Estimate`,
    value: `${value}${estimateTypeUnits[type]}`,
  };
}
