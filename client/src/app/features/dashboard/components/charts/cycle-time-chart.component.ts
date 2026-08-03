import { Component, computed, inject, input } from '@angular/core';
import { CycleTimeBucket } from '@core/models/reporting';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import {
  REPORT_CHART_LABEL_STYLE,
  formatReportValue,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { Store } from '@ngrx/store';
import { NgApexchartsModule } from 'ng-apexcharts';

@Component({
  selector: 'app-cycle-time-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      i18n-aria-label="Accessible name of the dashboard cycle time chart"
      aria-label="Cycle time by week"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis"
      [yaxis]="yaxis"
      [grid]="grid()"
      [stroke]="stroke"
      [markers]="markers"
      [tooltip]="tooltip()"
      [legend]="legend()"
      [dataLabels]="dataLabels" />
  `,
})
export class CycleTimeChartComponent {
  readonly buckets = input.required<CycleTimeBucket[]>();

  private readonly store = inject(Store);
  private readonly effectiveTheme =
    this.store.selectSignal(selectEffectiveTheme);
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => {
    const buckets = this.buckets();

    return [
      {
        name: $localize`:Series name for the middle cycle time value:Median`,
        data: buckets.map((bucket) => {
          return [
            new Date(bucket.weekStarting).getTime(),
            bucket.medianCycleTimeHours ?? null,
          ];
        }),
      },
      {
        name: $localize`:Series name for the 85th percentile cycle time:85th percentile`,
        data: buckets.map((bucket) => {
          return [
            new Date(bucket.weekStarting).getTime(),
            bucket.p85CycleTimeHours ?? null,
          ];
        }),
      },
    ];
  });

  readonly colors = computed(() => [
    this.theme().primary,
    this.theme().mutedForeground,
  ]);

  readonly grid = computed(() => ({
    borderColor: this.theme().border,
    strokeDashArray: 4,
    padding: { left: 0, right: 0 },
  }));

  readonly legend = computed(() => ({
    position: 'top' as const,
    horizontalAlign: 'right' as const,
    fontSize: '12px',
    markers: { size: 5 },
    labels: { colors: this.theme().mutedForeground },
  }));

  readonly tooltip = computed(() => ({
    shared: true,
    x: { format: 'dd MMM yyyy' },
    y: { formatter: (value: number) => `${formatReportValue(value)}h` },
    theme: this.effectiveTheme(),
  }));

  readonly chart = {
    type: 'line' as const,
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
      formatter: (value: number) => `${Math.round(value)}h`,
    },
  };

  readonly stroke = {
    width: [2.5, 1.5],
    dashArray: [0, 4],
    curve: 'smooth' as const,
  };

  readonly markers = { size: 0, hover: { size: 4 } };
  readonly dataLabels = { enabled: false };
}
