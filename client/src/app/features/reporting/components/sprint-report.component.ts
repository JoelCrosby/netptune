import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import {
  ReportingUnit,
  SprintBurndownReport,
  VelocityReport,
} from '@core/models/reporting';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
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
    StrokedButtonComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <app-section-header
        heading="Sprint reporting"
        description="Committed scope, burndown, and completed velocity." />

      @if (!sprintId()) {
        <app-empty-state
          compact
          title="No sprint selected"
          description="Select a sprint to view its burndown." />
      } @else if (burndown.isLoading()) {
        <div class="h-40">
          <app-page-loading label="Loading burndown" />
        </div>
      } @else if (burndown.error()) {
        <app-empty-state
          compact
          title="Burndown is unavailable"
          description="No reliable baseline is available for this sprint. Pre-coverage sprints are not approximated.">
          <button
            emptyStateAction
            app-stroked-button
            type="button"
            (click)="burndown.reload()">
            Retry
          </button>
        </app-empty-state>
      } @else if (burndown.value(); as report) {
        <app-report-coverage-notice [coverage]="report.coverage" />
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-5">
          <app-stat label="Committed" [value]="report.committedCount" />
          <app-stat label="Added" [value]="report.addedCount" />
          <app-stat label="Removed" [value]="report.removedCount" />
          <app-stat label="Completed" [value]="report.completedCount" />
          <app-stat
            label="Completion"
            [value]="report.completionPercentage + '%'" />
        </div>

        @if (shouldShowMissingEstimateWarning(report)) {
          <p class="text-muted text-sm">
            {{ report.missingEstimateCount }} current scope
            {{ report.missingEstimateCount === 1 ? 'item has' : 'items have' }}
            no compatible estimate and are excluded from numeric totals.
          </p>
        }

        <app-card>
          <app-card-header>
            <app-card-title>Burndown</app-card-title>
            <app-card-subtitle>
              Remaining scope compared with the ideal trajectory
            </app-card-subtitle>
          </app-card-header>
          <app-card-content>
            <app-sprint-burndown-chart [points]="report.points" />
          </app-card-content>
        </app-card>

        <app-table>
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th class="px-4 py-3">Date</th>
              <th class="px-4 py-3">Remaining</th>
              <th class="px-4 py-3">Total scope</th>
              <th class="px-4 py-3">Ideal</th>
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
          heading="Velocity"
          description="Committed and completed scope across recent sprints." />

        @if (velocity.isLoading()) {
          <div class="h-40">
            <app-page-loading label="Loading velocity" />
          </div>
        } @else if (velocity.error()) {
          <app-empty-state
            compact
            title="Velocity could not be loaded"
            description="Retry the request to load sprint velocity.">
            <button
              emptyStateAction
              app-stroked-button
              type="button"
              (click)="velocity.reload()">
              Retry
            </button>
          </app-empty-state>
        } @else if (velocity.value(); as report) {
          <app-report-coverage-notice [coverage]="report.coverage" />

          @if (report.sprints.length) {
            <app-card>
              <app-card-header>
                <app-card-title>Recent velocity</app-card-title>
                <app-card-subtitle>
                  Committed and completed sprint scope
                </app-card-subtitle>
              </app-card-header>
              <app-card-content>
                <app-sprint-velocity-chart [sprints]="report.sprints" />
              </app-card-content>
            </app-card>

            <app-table>
              <thead appTableHead>
                <tr appTableHeaderRow>
                  <th class="px-4 py-3">Sprint</th>
                  <th class="px-4 py-3">Committed</th>
                  <th class="px-4 py-3">Completed</th>
                  <th class="px-4 py-3">Missing estimate</th>
                  <th class="px-4 py-3">Different unit</th>
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
              title="No velocity data"
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
