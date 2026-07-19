import { httpResource } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import {
  EstimateType,
  estimateTypeUnits,
} from '@core/enums/estimate-type';
import { SprintBurndownReport } from '@core/models/reporting';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { LucideCalendarClock } from '@lucide/angular';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { StatComponent } from '@static/components/stat/stat.component';
import { SprintStatusClassesPipe } from '@app/features/sprints/pipes/sprint-status-classes.pipe';
import { SprintStatusLabelPipe } from '@app/features/sprints/pipes/sprint-status-label.pipe';
import { sprintDaysChip } from '@app/features/sprints/utils/sprint-days-chip';
import { SprintBurndownSparklineComponent } from './sprint-burndown-sparkline.component';

interface SprintStat {
  label: string;
  value: string | number;
}

const browserTimeZone =
  Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

@Component({
  selector: 'app-dashboard-current-sprint-card',
  imports: [
    DatePipe,
    RouterLink,
    SpinnerComponent,
    ProgressBarComponent,
    StatComponent,
    SprintBurndownSparklineComponent,
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

        @if (sprint.taskCount > 0) {
          <div>
            <div class="mb-2 flex items-baseline justify-between">
              <span class="text-foreground text-sm font-semibold">
                {{ progressPercent() }}% complete
              </span>
              <span class="text-muted text-sm">
                {{ sprint.doneTaskCount }} / {{ sprint.taskCount }} tasks
              </span>
            </div>
            <app-progress-bar [value]="progressPercent()" />
          </div>
        }

        <div class="grid gap-3 sm:grid-cols-3">
          @for (stat of stats(); track stat.label) {
            <app-stat compact [label]="stat.label" [value]="stat.value" />
          }
        </div>

        @if (burndownPoints().length > 1) {
          <div class="border-border/60 flex flex-col gap-2 border-t pt-4">
            <app-sprint-burndown-sparkline [points]="burndownPoints()" />
            <a
              class="text-primary self-end text-xs font-medium hover:underline"
              [routerLink]="['../reports']"
              [queryParams]="{ sprintId: sprint.id }">
              View report →
            </a>
          </div>
        }
      </section>
    }
  `,
})
export class DashboardCurrentSprintCardComponent {
  private readonly resource = httpResource<
    ClientResponse<SprintDetailViewModel | null>
  >(() => 'api/sprints/current');

  readonly sprint = computed(() => this.resource.value()?.payload ?? null);

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  // Sprint report data. Fails quietly (pre-coverage sprint, no baseline, or the viewer lacks
  // reporting access) — the card still renders from the sprint stats above.
  private readonly burndown = httpResource<SprintBurndownReport>(() => {
    const sprint = this.sprint();
    return sprint
      ? `api/reports/sprints/${sprint.id}/burndown?unit=Tasks&timeZone=${encodeURIComponent(browserTimeZone)}`
      : undefined;
  });

  readonly burndownPoints = computed(() => this.burndown.value()?.points ?? []);

  readonly progressPercent = computed(() => {
    const sprint = this.sprint();
    if (!sprint?.taskCount) return 0;
    return Math.round((sprint.doneTaskCount / sprint.taskCount) * 100);
  });

  readonly stats = computed<SprintStat[]>(() => {
    const sprint = this.sprint();
    if (!sprint) return [];

    const tiles: SprintStat[] = [
      { label: 'Remaining', value: sprint.taskCount - sprint.doneTaskCount },
    ];

    const report = this.burndown.value();
    if (report) {
      tiles.push({ label: 'Scope', value: scopeLabel(report) });
    }

    const estimate = estimateStat(sprint);
    if (estimate) {
      tiles.push(estimate);
    }

    if (tiles.length < 2) {
      tiles.push({ label: 'Completed', value: sprint.doneTaskCount });
    }

    return tiles;
  });

  readonly daysChip = computed(() => {
    const sprint = this.sprint();
    return sprint ? sprintDaysChip(sprint.status, sprint.endDate) : null;
  });
}

function scopeLabel(report: SprintBurndownReport): string {
  const net = report.addedCount - report.removedCount;
  if (net > 0) return `${report.committedCount} (+${net})`;
  if (net < 0) return `${report.committedCount} (${net})`;
  return `${report.committedCount}`;
}

function estimateStat(sprint: SprintDetailViewModel): SprintStat | null {
  const type = sprint.estimateType;
  const value = sprint.totalEstimateValue;

  // Story points and hours are the only numeric estimate units; t-shirt sizes are categorical.
  const isNumericUnit =
    type === EstimateType.storyPoints || type === EstimateType.hours;

  if (!isNumericUnit || value == null) return null;

  return { label: 'Estimate', value: `${value}${estimateTypeUnits[type]}` };
}
