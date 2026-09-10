import { Component, computed, input, signal } from '@angular/core';
import { LucideTimer, LucideTrendingUp } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { FlowCycleTimeChartComponent } from './charts/flow-cycle-time-chart.component';
import { FlowThroughputChartComponent } from './charts/flow-throughput-chart.component';
import { FlowCycleTimeTableComponent } from './flow-cycle-time-table.component';
import { FlowThroughputTableComponent } from './flow-throughput-table.component';
import { hoursLabel } from './report-format';
import { ReportCoverageNoticeComponent } from './report-coverage-notice.component';
import { flowReportResource } from '@core/resources/reporting.resource';
import { PanelComponent } from '@static/components/panel.component';

@Component({
  selector: 'app-flow-report',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    FlowCycleTimeChartComponent,
    FlowCycleTimeTableComponent,
    FlowThroughputChartComponent,
    FlowThroughputTableComponent,
    PanelComponent,
    ReportCoverageNoticeComponent,
    SectionHeaderComponent,
    SkeletonComponent,
    StatStripComponent,
    StrokedButtonComponent,
  ],
  template: `
    <section class="flex flex-col gap-6">
      <app-section-header
        i18n-heading="Section heading for flow metrics"
        heading="Flow"
        i18n-description="Explains what flow metrics show"
        description="Completed work and elapsed cycle time." />

      @if (resource.isLoading()) {
        <app-panel
          surface="card"
          class="p-6"
          role="status"
          i18n-aria-label="Shown while flow metrics load"
          aria-label="Loading flow metrics">
          <app-skeleton class="h-10 w-full" />
          <app-skeleton class="mt-6 h-52 w-full" />
        </app-panel>
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

        <section app-panel surface="card">
          <app-stat-strip [items]="stats()" />
        </section>

        @if (report.buckets.length) {
          <app-chart-card
            [flush]="true"
            [icon]="throughputIcon"
            i18n-title="Heading of the throughput chart card"
            title="Throughput"
            i18n-description="Subheading of the throughput chart card"
            description="Completed tasks over time">
            <button
              chartCardActions
              app-stroked-button
              color="neutral"
              type="button"
              class="h-9 px-3 text-sm font-normal"
              id="throughput-data-toggle"
              aria-controls="throughput-data"
              [attr.aria-expanded]="showData()"
              (click)="showData.set(!showData())">
              @if (showData()) {
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
              <app-flow-throughput-chart [buckets]="report.buckets" />
            </div>

            @if (showData()) {
              <app-flow-throughput-table
                id="throughput-data"
                role="region"
                aria-labelledby="throughput-data-toggle"
                [query]="query()" />
            }
          </app-chart-card>

          @if (report.cycleTimeBuckets.length) {
            <app-chart-card
              [flush]="true"
              [icon]="cycleTimeIcon"
              i18n-title="Heading of the cycle-time chart card"
              title="Cycle-time trend"
              [description]="cycleTimeDescription()">
              <button
                chartCardActions
                app-stroked-button
                color="neutral"
                type="button"
                class="h-9 px-3 text-sm font-normal"
                id="cycle-time-data-toggle"
                aria-controls="cycle-time-data"
                [attr.aria-expanded]="showCycleTimeData()"
                (click)="showCycleTimeData.set(!showCycleTimeData())">
                @if (showCycleTimeData()) {
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
                <app-flow-cycle-time-chart
                  [buckets]="report.cycleTimeBuckets" />
              </div>

              @if (showCycleTimeData()) {
                <app-flow-cycle-time-table
                  id="cycle-time-data"
                  role="region"
                  aria-labelledby="cycle-time-data-toggle"
                  [query]="query()" />
              }
            </app-chart-card>
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
  readonly resource = flowReportResource(
    computed(() => Object.fromEntries(new URLSearchParams(this.query())))
  );

  protected readonly showData = signal(false);
  protected readonly showCycleTimeData = signal(false);

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
