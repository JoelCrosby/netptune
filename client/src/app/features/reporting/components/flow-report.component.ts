import { httpResource } from '@angular/common/http';
import { Component, computed, input } from '@angular/core';
import { FlowReport } from '@core/models/reporting';
import { LucideTimer, LucideTrendingUp } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import {
  TableComponent,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { FlowCycleTimeChartComponent } from './charts/flow-cycle-time-chart.component';
import { FlowThroughputChartComponent } from './charts/flow-throughput-chart.component';
import { ReportCoverageNoticeComponent } from './report-coverage-notice.component';

function hoursLabel(value?: number | null): string {
  return value == null ? '—' : `${Math.round(value * 10) / 10}h`;
}

@Component({
  selector: 'app-flow-report',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    FlowThroughputChartComponent,
    FlowCycleTimeChartComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
    SkeletonComponent,
    StatStripComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <section class="flex flex-col gap-6">
      <app-section-header
        i18n-heading="Section heading for flow metrics"
        heading="Flow"
        i18n-description="Explains what flow metrics show"
        description="Completed work and elapsed cycle time." />

      @if (resource.isLoading()) {
        <div
          class="border-border bg-card rounded-lg border p-6 shadow-sm"
          role="status"
          i18n-aria-label="Shown while flow metrics load"
          aria-label="Loading flow metrics">
          <app-skeleton class="h-10 w-full" />
          <app-skeleton class="mt-6 h-52 w-full" />
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

        <section
          class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
          <app-stat-strip [items]="stats()" />
        </section>

        @if (report.buckets.length) {
          <app-chart-card
            [icon]="throughputIcon"
            i18n-title="Heading of the throughput chart card"
            title="Throughput"
            i18n-description="Subheading of the throughput chart card"
            description="Completed tasks over time">
            <app-flow-throughput-chart [buckets]="report.buckets" />
          </app-chart-card>

          <app-table containerClass="overflow-x-auto rounded-lg shadow-sm">
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
                  <td class="px-4 py-2.5 tabular-nums">
                    {{ bucket.completed }}
                  </td>
                </tr>
              }
            </tbody>
          </app-table>

          @if (report.cycleTimeBuckets.length) {
            <app-chart-card
              [icon]="cycleTimeIcon"
              i18n-title="Heading of the cycle-time chart card"
              title="Cycle-time trend"
              [description]="cycleTimeDescription()">
              <app-flow-cycle-time-chart [buckets]="report.cycleTimeBuckets" />
            </app-chart-card>

            <app-table containerClass="overflow-x-auto rounded-lg shadow-sm">
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
                    <td class="px-4 py-2.5 tabular-nums">
                      {{ hours(bucket.medianCycleTimeHours) }}
                    </td>
                    <td class="px-4 py-2.5 tabular-nums">
                      {{ hours(bucket.p85CycleTimeHours) }}
                    </td>
                    <td class="px-4 py-2.5 tabular-nums">
                      {{ bucket.sampleSize }}
                    </td>
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

  protected readonly throughputIcon = LucideTrendingUp;
  protected readonly cycleTimeIcon = LucideTimer;

  protected readonly stats = computed<StatStripItem[]>(() => {
    const report = this.resource.value();

    if (!report) return [];

    return [
      {
        label: $localize`:Stat label for completed tasks:Completed`,
        value: report.throughput,
      },
      {
        label: $localize`:Stat label for the median cycle time:Median cycle`,
        value: hoursLabel(report.medianCycleTimeHours),
      },
      {
        label: $localize`:Stat label for the 85th percentile cycle time:85th percentile`,
        value: hoursLabel(report.p85CycleTimeHours),
      },
      {
        label: $localize`:Stat label for tasks still open:Current open tasks`,
        value: report.currentOpenTaskCount,
      },
    ];
  });

  protected readonly cycleTimeDescription = computed(() => {
    const samples = this.resource.value()?.cycleTimeSampleSize ?? 0;

    return $localize`:Subheading of the cycle-time chart card. COUNT is how many completed cycles the figures are based on:Weekly median and 85th percentile from ${samples}:COUNT: completed cycle samples`;
  });

  hours(value?: number | null): string {
    return hoursLabel(value);
  }
}
