import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import {
  ReportingUnit,
  SprintBurndownReport,
  VelocityReport,
} from '@core/models/reporting';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { StatComponent } from '@static/components/stat/stat.component';
import {
  TableComponent,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { SprintBurndownChartComponent } from './charts/sprint-burndown-chart.component';
import { SprintVelocityChartComponent } from './charts/sprint-velocity-chart.component';
import { ReportCoverageNoticeComponent } from './report-coverage-notice.component';
import { formatReportValue } from '../utils/report-chart-theme';

@Component({
  selector: 'app-sprint-report',
  imports: [
    ErrorStateComponent,
    CardComponent,
    CardContentComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    EmptyStateComponent,
    PageLoadingComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
    SprintBurndownChartComponent,
    SprintVelocityChartComponent,
    StatComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
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
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-5">
          <app-stat
            i18n-label="Stat label for scope committed at sprint start"
            label="Committed"
            [value]="report.committedCount" />
          <app-stat
            i18n-label="Stat label for scope added mid-sprint"
            label="Added"
            [value]="report.addedCount" />
          <app-stat
            i18n-label="Stat label for scope removed mid-sprint"
            label="Removed"
            [value]="report.removedCount" />
          <app-stat
            i18n-label="Stat label for completed scope"
            label="Completed"
            [value]="report.completedCount" />
          <app-stat
            i18n-label="Stat label for the completion percentage"
            label="Completion"
            [value]="report.completionPercentage + '%'" />
        </div>

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

        <app-card>
          <app-card-header>
            <app-card-title i18n="Heading of the burndown chart card">
              Burndown
            </app-card-title>
            <app-card-subtitle>
              <span i18n="Subheading of the burndown chart card">
                Remaining scope compared with the ideal trajectory
              </span>
            </app-card-subtitle>
          </app-card-header>
          <app-card-content>
            <app-sprint-burndown-chart [points]="report.points" />
          </app-card-content>
        </app-card>

        <app-table containerClass="overflow-x-auto">
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th class="px-4 py-3">
                <span i18n="Column heading for the date">Date</span>
              </th>
              <th class="px-4 py-3">
                <span i18n="Column heading for remaining scope">Remaining</span>
              </th>
              <th class="px-4 py-3">
                <span i18n="Column heading for total scope">Total scope</span>
              </th>
              <th class="px-4 py-3">
                <span i18n="Column heading for the ideal burndown value">
                  Ideal
                </span>
              </th>
            </tr>
          </thead>
          <tbody>
            @for (point of report.points; track point.date) {
              <tr appTableRow>
                <td class="px-4 py-2.5">{{ point.date }}</td>
                <td class="px-4 py-2.5">
                  {{ formatValue(point.remaining) }}
                </td>
                <td class="px-4 py-2.5">
                  {{ formatValue(point.totalScope) }}
                </td>
                <td class="px-4 py-2.5">
                  {{ formatValue(point.ideal) }}
                </td>
              </tr>
            }
          </tbody>
        </app-table>
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
            <app-card>
              <app-card-header>
                <app-card-title i18n="Heading of the velocity chart card">
                  Recent velocity
                </app-card-title>
                <app-card-subtitle>
                  <span i18n="Subheading of the velocity chart card">
                    Committed and completed sprint scope
                  </span>
                </app-card-subtitle>
              </app-card-header>
              <app-card-content>
                <app-sprint-velocity-chart [sprints]="report.sprints" />
              </app-card-content>
            </app-card>

            <app-table containerClass="overflow-x-auto">
              <thead appTableHead>
                <tr appTableHeaderRow>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for the sprint name">
                      Sprint
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for committed scope">
                      Committed
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for completed scope">
                      Completed
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for tasks without an estimate">
                      Missing estimate
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span
                      i18n="Column heading for tasks estimated in another unit">
                      Different unit
                    </span>
                  </th>
                </tr>
              </thead>
              <tbody>
                @for (point of report.sprints; track point.sprintId) {
                  <tr appTableRow>
                    <td class="px-4 py-2.5 font-medium">
                      {{ point.sprintName }}
                    </td>
                    <td class="px-4 py-2.5">
                      {{ formatValue(point.committed) }}
                    </td>
                    <td class="px-4 py-2.5">
                      {{ formatValue(point.completed) }}
                    </td>
                    <td class="px-4 py-2.5">
                      {{ point.missingEstimateCount }}
                    </td>
                    <td class="px-4 py-2.5">
                      {{ point.differentUnitEstimateCount }}
                    </td>
                  </tr>
                }
              </tbody>
            </app-table>
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
  readonly formatValue = formatReportValue;
  readonly sprintId = input<number>();
  readonly projectId = input<number>();
  readonly unit = input.required<ReportingUnit>();
  readonly timeZone = input.required<string>();
  readonly burndown = httpResource<SprintBurndownReport>(() => {
    const sprintId = this.sprintId();
    return sprintId
      ? `api/reports/sprints/${sprintId}/burndown?unit=${this.unit()}&timeZone=${encodeURIComponent(this.timeZone())}`
      : undefined;
  });
  readonly velocity = httpResource<VelocityReport>(() => {
    const projectId = this.projectId();
    return projectId
      ? `api/reports/velocity?projectId=${projectId}&unit=${this.unit()}&take=12`
      : undefined;
  });

  shouldShowMissingEstimateWarning(report: SprintBurndownReport): boolean {
    const hasMissingEstimates = report.missingEstimateCount > 0;
    const usesEstimatedUnit = report.unit !== 'Tasks';

    return hasMissingEstimates && usesEstimatedUnit;
  }
}
