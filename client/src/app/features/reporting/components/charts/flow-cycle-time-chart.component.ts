import { Component, computed, input } from '@angular/core';
import { CycleTimeBucket } from '@core/models/reporting';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '../../utils/report-chart-theme';

@Component({
  selector: 'app-flow-cycle-time-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      aria-label="Weekly median and 85th-percentile cycle time"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis"
      [yaxis]="yaxis"
      [grid]="grid()"
      [legend]="legend()"
      [stroke]="stroke"
      [dataLabels]="dataLabels" />
  `,
})
export class FlowCycleTimeChartComponent {
  readonly buckets = input.required<CycleTimeBucket[]>();
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: 'Median',
      data: this.buckets().map((bucket) => [
        new Date(bucket.weekStarting).getTime(),
        bucket.medianCycleTimeHours,
      ]),
    },
    {
      name: '85th percentile',
      data: this.buckets().map((bucket) => [
        new Date(bucket.weekStarting).getTime(),
        bucket.p85CycleTimeHours,
      ]),
    },
  ]);
  readonly colors = computed(() => [
    this.theme().primary,
    this.theme().mutedForeground,
  ]);
  readonly grid = computed(() => ({ borderColor: this.theme().border }));
  readonly legend = computed(() => ({
    labels: { colors: this.theme().mutedForeground },
  }));
  readonly chart = {
    type: 'line' as const,
    height: 260,
    toolbar: { show: false },
  };
  readonly xaxis = {
    type: 'datetime' as const,
    labels: { style: REPORT_CHART_LABEL_STYLE },
  };
  readonly yaxis = {
    min: 0,
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      formatter: (value: number) => `${Math.round(value * 10) / 10}h`,
    },
  };
  readonly stroke = { width: [3, 2], curve: 'straight' as const };
  readonly dataLabels = { enabled: false };
}
