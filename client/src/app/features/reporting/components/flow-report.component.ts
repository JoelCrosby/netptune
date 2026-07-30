import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import { FlowReport } from '@core/models/reporting';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { StatComponent } from '@static/components/stat/stat.component';
import {
  TableComponent,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { FlowThroughputChartComponent } from './charts/flow-throughput-chart.component';
import { FlowCycleTimeChartComponent } from './charts/flow-cycle-time-chart.component';
import { ReportCoverageNoticeComponent } from './report-coverage-notice.component';

@Component({
  selector: 'app-flow-report',
  imports: [
    CardComponent,
    CardContentComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    FlowThroughputChartComponent,
    FlowCycleTimeChartComponent,
    PageLoadingComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
    StatComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <app-section-header
        i18n-heading="Section heading for flow metrics"
        heading="Flow"
        i18n-description="Explains what flow metrics show"
        description="Completed work and elapsed cycle time." />

      @if (resource.isLoading()) {
        <div class="h-40">
          <app-page-loading
            i18n-label="Shown while flow metrics load"
            label="Loading flow metrics" />
        </div>
      } @else if (resource.error()) {
        <app-error-state
          compact
          i18n-title="Shown when flow metrics fail to load"
          title="Flow metrics could not be loaded"
          i18n-description="Advice when flow metrics fail to load"
          description="Retry the request to load flow reporting."
          (retry)="resource.reload()" />
      } @else if (resource.value(); as report) {
        <app-report-coverage-notice [coverage]="report.coverage" />
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <app-stat
            i18n-label="Stat label for completed tasks"
            label="Completed"
            [value]="report.throughput" />
          <app-stat
            i18n-label="Stat label for the median cycle time"
            label="Median cycle"
            [value]="hours(report.medianCycleTimeHours)" />
          <app-stat
            label="85th percentile"
            [value]="hours(report.p85CycleTimeHours)" />
          <app-stat
            i18n-label="Stat label for tasks still open"
            label="Current open tasks"
            [value]="report.currentOpenTaskCount" />
        </div>

        @if (report.buckets.length) {
          <app-card>
            <app-card-header>
              <app-card-title i18n="Heading of the throughput chart card">
                Throughput
              </app-card-title>
              <app-card-subtitle i18n="Subheading of the throughput chart card">
                Completed tasks over time
              </app-card-subtitle>
            </app-card-header>
            <app-card-content>
              <app-flow-throughput-chart [buckets]="report.buckets" />
            </app-card-content>
          </app-card>

          <app-table containerClass="overflow-x-auto">
            <thead appTableHead>
              <tr appTableHeaderRow>
                <th class="px-4 py-3">
                  <span i18n="Column heading for the date">Date</span>
                </th>
                <th class="px-4 py-3">
                  <span i18n="Column heading for the completed count">
                    Completed
                  </span>
                </th>
              </tr>
            </thead>
            <tbody>
              @for (bucket of report.buckets; track bucket.date) {
                <tr appTableRow>
                  <td class="px-4 py-2.5">{{ bucket.date }}</td>
                  <td class="px-4 py-2.5">{{ bucket.completed }}</td>
                </tr>
              }
            </tbody>
          </app-table>

          @if (report.cycleTimeBuckets.length) {
            <app-card>
              <app-card-header>
                <app-card-title i18n="Heading of the cycle-time chart card">
                  Cycle-time trend
                </app-card-title>
                <app-card-subtitle>
                  <span
                    i18n="
                      Subheading of the cycle-time chart card. COUNT is how many
                      completed cycles the figures are based on
                    ">
                    Weekly median and 85th percentile from
                    {{
                      report.cycleTimeSampleSize // i18n(ph="COUNT")
                    }}
                    completed cycle samples
                  </span>
                </app-card-subtitle>
              </app-card-header>
              <app-card-content>
                <app-flow-cycle-time-chart
                  [buckets]="report.cycleTimeBuckets" />
              </app-card-content>
            </app-card>

            <app-table containerClass="overflow-x-auto">
              <thead appTableHead>
                <tr appTableHeaderRow>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for the week start date">
                      Week starting
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for the median cycle time">
                      Median
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span
                      i18n="Column heading for the 85th percentile cycle time">
                      85th percentile
                    </span>
                  </th>
                  <th class="px-4 py-3">
                    <span i18n="Column heading for the number of samples">
                      Samples
                    </span>
                  </th>
                </tr>
              </thead>
              <tbody>
                @for (
                  bucket of report.cycleTimeBuckets;
                  track bucket.weekStarting
                ) {
                  <tr appTableRow>
                    <td class="px-4 py-2.5">{{ bucket.weekStarting }}</td>
                    <td class="px-4 py-2.5">
                      {{ hours(bucket.medianCycleTimeHours) }}
                    </td>
                    <td class="px-4 py-2.5">
                      {{ hours(bucket.p85CycleTimeHours) }}
                    </td>
                    <td class="px-4 py-2.5">{{ bucket.sampleSize }}</td>
                  </tr>
                }
              </tbody>
            </app-table>
          }
        } @else {
          <app-empty-state
            compact
            i18n-title="Empty state for flow metrics"
            title="No completed work"
            i18n-description="Explains the empty flow metrics state"
            description="No completions were recorded in this period." />
        }
      }
    </section>
  `,
})
export class FlowReportComponent {
  readonly query = input.required<string>();
  readonly resource = httpResource<FlowReport>(
    () => `api/reports/flow?${this.query()}`
  );

  hours(value?: number | null): string {
    return value == null ? '—' : `${Math.round(value * 10) / 10}h`;
  }
}
