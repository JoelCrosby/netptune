import { Component, computed, input, signal } from '@angular/core';
import { ReportingUnit, SprintBurndownReport } from '@core/models/reporting';
import { LucideChartColumnBig, LucideTrendingDown } from '@lucide/angular';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { SprintBurndownChartComponent } from './charts/sprint-burndown-chart.component';
import { SprintVelocityChartComponent } from './charts/sprint-velocity-chart.component';
import { SprintBurndownTableComponent } from './sprint-burndown-table.component';
import { SprintVelocityTableComponent } from './sprint-velocity-table.component';
import { ReportCoverageNoticeComponent } from './report-coverage-notice.component';
import { PanelComponent } from '@static/components/panel.component';
import {
  sprintBurndownResource,
  velocityReportResource,
} from '@core/resources/reporting.resource';

const recentSprints = 12;

@Component({
  selector: 'app-sprint-report',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PageLoadingComponent,
    PanelComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
    SprintBurndownChartComponent,
    SprintBurndownTableComponent,
    SprintVelocityChartComponent,
    SprintVelocityTableComponent,
    StatStripComponent,
    StrokedButtonComponent,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <app-section-header
        i18n-heading="Section heading for sprint reports"
        heading="Sprint reporting"
        i18n-description="Explains what sprint reports show"
        description="Committed scope, burndown, and completed velocity." />

      @if (!sprintId()) {
        <app-empty-state
          compact
          i18n-title="Shown before a sprint is chosen"
          title="No sprint selected"
          i18n-description="Prompts the user to choose a sprint"
          description="Select a sprint to view its burndown." />
      } @else if (burndown.isLoading()) {
        <div class="h-40">
          <app-page-loading
            i18n-label="Shown while the burndown loads"
            label="Loading burndown" />
        </div>
      } @else if (burndown.error()) {
        <app-error-state
          compact
          i18n-title="Shown when a sprint has no burndown baseline"
          title="Burndown is unavailable"
          i18n-description="Explains why a burndown is unavailable"
          description="No reliable baseline is available for this sprint. Pre-coverage sprints are not approximated."
          (retry)="burndown.reload()" />
      } @else if (burndown.value(); as report) {
        <app-report-coverage-notice [coverage]="report.coverage" />
        <section app-panel surface="card">
          <app-stat-strip [items]="burndownStats(report)" />
        </section>

        @if (shouldShowMissingEstimateWarning(report)) {
          <p class="text-muted text-sm">
            <ng-container
              i18n="Warns how many in-scope items lack a compatible estimate">
              {report.missingEstimateCount, plural,
                =1 {
                  1 current scope item has no compatible estimate and is
                  excluded from numeric totals.
                }
                other {
                  {{ report.missingEstimateCount }} current scope items have no
                  compatible estimate and are excluded from numeric totals.
                }
              }
            </ng-container>
          </p>
        }

        <app-chart-card
          [flush]="true"
          [icon]="burndownIcon"
          i18n-title="Heading of the burndown chart card"
          title="Burndown"
          i18n-description="Subheading of the burndown chart card"
          description="Remaining scope compared with the ideal trajectory">
          <button
            chartCardActions
            app-stroked-button
            color="neutral"
            type="button"
            class="h-9 px-3 text-sm font-normal"
            id="burndown-data-toggle"
            aria-controls="burndown-data"
            [attr.aria-expanded]="showBurndownData()"
            (click)="showBurndownData.set(!showBurndownData())">
            @if (showBurndownData()) {
              <span i18n="Button that hides the throughput breakdown table">
                Hide data
              </span>
            } @else {
              <span i18n="Button that reveals the throughput breakdown table">
                Show data
              </span>
            }
          </button>

          <div class="px-6 py-5">
            <app-sprint-burndown-chart [points]="report.points" />
          </div>

          @if (showBurndownData(); as show) {
            <app-sprint-burndown-table
              id="burndown-data"
              role="region"
              aria-labelledby="burndown-data-toggle"
              [sprintId]="report.sprintId"
              [unit]="unit()"
              [timeZone]="timeZone()" />
          }
        </app-chart-card>
      }

      @if (projectId()) {
        <app-section-header
          class="mt-6"
          i18n-heading="Section heading for sprint velocity"
          heading="Velocity"
          i18n-description="Explains what velocity shows"
          description="Committed and completed scope across recent sprints." />

        @if (velocity.isLoading()) {
          <div class="h-40">
            <app-page-loading
              i18n-label="Shown while velocity loads"
              label="Loading velocity" />
          </div>
        } @else if (velocity.error()) {
          <app-error-state
            compact
            i18n-title="Shown when velocity fails to load"
            title="Velocity could not be loaded"
            i18n-description="Advice when velocity fails to load"
            description="Retry the request to load sprint velocity."
            (retry)="velocity.reload()" />
        } @else if (velocity.value(); as report) {
          <app-report-coverage-notice [coverage]="report.coverage" />

          @if (report.sprints.length) {
            <app-chart-card
              [flush]="true"
              [icon]="velocityIcon"
              i18n-title="Heading of the velocity chart card"
              title="Recent velocity"
              i18n-description="Subheading of the velocity chart card"
              description="Committed and completed sprint scope">
              <button
                chartCardActions
                app-stroked-button
                color="neutral"
                type="button"
                class="h-9 px-3 text-sm font-normal"
                id="velocity-data-toggle"
                aria-controls="velocity-data"
                [attr.aria-expanded]="showVelocityData()"
                (click)="showVelocityData.set(!showVelocityData())">
                @if (showVelocityData()) {
                  <span i18n="Button that hides the throughput breakdown table">
                    Hide data
                  </span>
                } @else {
                  <span
                    i18n="Button that reveals the throughput breakdown table">
                    Show data
                  </span>
                }
              </button>

              <div class="px-6 py-5">
                <app-sprint-velocity-chart [sprints]="report.sprints" />
              </div>

              @if (showVelocityData() && projectId(); as project) {
                <app-sprint-velocity-table
                  id="velocity-data"
                  role="region"
                  aria-labelledby="velocity-data-toggle"
                  [projectId]="project"
                  [unit]="unit()"
                  [take]="recentSprints" />
              }
            </app-chart-card>
          } @else {
            <app-empty-state
              compact
              i18n-title="Empty state for sprint velocity"
              title="No velocity data"
              i18n-description="Explains the empty velocity state"
              description="No completed, post-coverage sprints are available." />
          }
        }
      }
    </section>
  `,
})
export class SprintReportComponent {
  protected readonly recentSprints = recentSprints;
  protected readonly showBurndownData = signal(false);
  protected readonly showVelocityData = signal(false);

  protected readonly burndownIcon = LucideTrendingDown;
  protected readonly velocityIcon = LucideChartColumnBig;

  protected burndownStats(report: SprintBurndownReport): StatStripItem[] {
    return [
      {
        label: $localize`:Stat label for scope committed at sprint start:Committed`,
        value: report.committedCount,
      },
      {
        label: $localize`:Stat label for scope added mid-sprint:Added`,
        value: report.addedCount,
      },
      {
        label: $localize`:Stat label for scope removed mid-sprint:Removed`,
        value: report.removedCount,
      },
      {
        label: $localize`:Stat label for completed scope:Completed`,
        value: report.completedCount,
      },
      {
        label: $localize`:Stat label for the completion percentage:Completion`,
        value: `${report.completionPercentage}%`,
      },
    ];
  }

  readonly sprintId = input<number>();
  readonly projectId = input<number>();
  readonly unit = input.required<ReportingUnit>();
  readonly timeZone = input.required<string>();
  readonly burndown = sprintBurndownResource(
    this.sprintId,
    computed(() => ({ unit: this.unit(), timeZone: this.timeZone() }))
  );
  readonly velocity = velocityReportResource(
    this.projectId,
    computed(() => ({ unit: this.unit(), take: recentSprints }))
  );

  shouldShowMissingEstimateWarning(report: SprintBurndownReport): boolean {
    const hasMissingEstimates = report.missingEstimateCount > 0;
    const usesEstimatedUnit = report.unit !== 'Tasks';

    return hasMissingEstimates && usesEstimatedUnit;
  }
}
