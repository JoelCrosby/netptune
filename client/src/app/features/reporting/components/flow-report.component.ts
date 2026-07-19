import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import { FlowReport } from '@core/models/reporting';
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
    FlowThroughputChartComponent,
    FlowCycleTimeChartComponent,
    PageLoadingComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
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
        heading="Flow"
        description="Completed work and elapsed cycle time." />

      @if (resource.isLoading()) {
        <div class="h-40">
          <app-page-loading label="Loading flow metrics" />
        </div>
      } @else if (resource.error()) {
        <app-empty-state
          compact
          title="Flow metrics could not be loaded"
          description="Retry the request to load flow reporting.">
          <button
            emptyStateAction
            app-stroked-button
            type="button"
            (click)="resource.reload()">
            Retry
          </button>
        </app-empty-state>
      } @else if (resource.value(); as report) {
        <app-report-coverage-notice [coverage]="report.coverage" />
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <app-stat label="Completed" [value]="report.throughput" />
          <app-stat
            label="Median cycle"
            [value]="hours(report.medianCycleTimeHours)" />
          <app-stat
            label="85th percentile"
            [value]="hours(report.p85CycleTimeHours)" />
          <app-stat
            label="Current open tasks"
            [value]="report.currentOpenTaskCount" />
        </div>

        @if (report.buckets.length) {
          <app-card>
            <app-card-header>
              <app-card-title>Throughput</app-card-title>
              <app-card-subtitle>Completed tasks over time</app-card-subtitle>
            </app-card-header>
            <app-card-content>
              <app-flow-throughput-chart [buckets]="report.buckets" />
            </app-card-content>
          </app-card>

          <app-table containerClass="overflow-x-auto">
            <thead appTableHead>
              <tr appTableHeaderRow>
                <th class="px-4 py-3">Date</th>
                <th class="px-4 py-3">Completed</th>
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
                <app-card-title>Cycle-time trend</app-card-title>
                <app-card-subtitle>
                  Weekly median and 85th percentile from
                  {{ report.cycleTimeSampleSize }} completed cycle samples
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
                  <th class="px-4 py-3">Week starting</th>
                  <th class="px-4 py-3">Median</th>
                  <th class="px-4 py-3">85th percentile</th>
                  <th class="px-4 py-3">Samples</th>
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
            title="No completed work"
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
