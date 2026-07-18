import { Component, computed, input } from '@angular/core';
import { FlowBucket } from '@core/models/reporting';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '../../utils/report-chart-theme';

@Component({
  selector: 'app-flow-throughput-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      aria-label="Completed tasks over time"
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
export class FlowThroughputChartComponent {
  readonly buckets = input.required<FlowBucket[]>();
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: 'Completed',
      data: this.buckets().map((bucket) => [
        new Date(bucket.date).getTime(),
        bucket.completed,
      ]),
    },
  ]);
  readonly colors = computed(() => [this.theme().primary]);
  readonly grid = computed(() => ({ borderColor: this.theme().border }));
  readonly legend = computed(() => ({
    labels: { colors: this.theme().mutedForeground },
  }));
  readonly chart = {
    type: 'bar' as const,
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
      formatter: (value: number) => Math.floor(value).toString(),
    },
  };
  readonly stroke = { width: 2 };
  readonly dataLabels = { enabled: false };
}
