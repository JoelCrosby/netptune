import { Component, computed, inject, input } from '@angular/core';
import { FlowBucket } from '@core/models/reporting';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { Store } from '@ngrx/store';
import { NgApexchartsModule } from 'ng-apexcharts';

@Component({
  selector: 'app-throughput-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      i18n-aria-label="Accessible name of the dashboard throughput chart"
      aria-label="Tasks completed each day"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis"
      [yaxis]="yaxis"
      [grid]="grid()"
      [stroke]="stroke"
      [fill]="fill"
      [markers]="markers"
      [tooltip]="tooltip()"
      [legend]="legend"
      [dataLabels]="dataLabels" />
  `,
})
export class ThroughputChartComponent {
  readonly buckets = input.required<FlowBucket[]>();

  private readonly store = inject(Store);
  private readonly effectiveTheme =
    this.store.selectSignal(selectEffectiveTheme);
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: $localize`:Series name for tasks finished each day:Completed`,
      data: this.buckets().map((bucket) => {
        return [new Date(bucket.date).getTime(), bucket.completed];
      }),
    },
  ]);

  readonly colors = computed(() => [this.theme().primary]);
  readonly grid = computed(() => ({
    borderColor: this.theme().border,
    strokeDashArray: 4,
    padding: { left: 0, right: 0 },
  }));

  readonly tooltip = computed(() => ({
    x: { format: 'ddd dd MMM' },
    theme: this.effectiveTheme(),
  }));

  readonly chart = {
    type: 'area' as const,
    height: 210,
    toolbar: { show: false },
    zoom: { enabled: false },
    background: 'transparent',
    animations: { enabled: false },
  };

  readonly xaxis = {
    type: 'datetime' as const,
    labels: { style: REPORT_CHART_LABEL_STYLE, datetimeUTC: false },
    axisBorder: { show: false },
    axisTicks: { show: false },
  };

  readonly yaxis = {
    min: 0,
    forceNiceScale: true,
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      formatter: (value: number) => Math.floor(value).toString(),
    },
  };

  readonly stroke = { width: 2, curve: 'smooth' as const };

  readonly fill = {
    type: 'gradient' as const,
    gradient: {
      type: 'vertical' as const,
      shadeIntensity: 0,
      opacityFrom: 0.4,
      opacityTo: 0,
      stops: [0, 100],
    },
  };

  readonly markers = { size: 0, hover: { size: 4 } };
  readonly legend = { show: false };
  readonly dataLabels = { enabled: false };
}
