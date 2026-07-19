import { Component, computed, input } from '@angular/core';
import { BurndownPoint } from '@core/models/reporting';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  REPORT_CHART_LABEL_STYLE,
  formatReportValue,
  reportChartThemeSignal,
} from '../../utils/report-chart-theme';

@Component({
  selector: 'app-sprint-burndown-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      aria-label="Sprint remaining work and ideal burndown"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis"
      [yaxis]="yaxis"
      [grid]="grid()"
      [legend]="legend()"
      [stroke]="stroke"
      [tooltip]="tooltip"
      [dataLabels]="dataLabels" />
  `,
})
export class SprintBurndownChartComponent {
  readonly points = input.required<BurndownPoint[]>();
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: 'Remaining',
      data: this.points().map((point) => [
        new Date(point.date).getTime(),
        point.remaining,
      ]),
    },
    {
      name: 'Total scope',
      data: this.points().map((point) => [
        new Date(point.date).getTime(),
        point.totalScope,
      ]),
    },
    {
      name: 'Ideal',
      data: this.points().map((point) => [
        new Date(point.date).getTime(),
        point.ideal,
      ]),
    },
  ]);
  readonly colors = computed(() => [
    this.theme().primary,
    this.theme().foreground,
    this.theme().mutedForeground,
  ]);
  readonly grid = computed(() => ({ borderColor: this.theme().border }));
  readonly legend = computed(() => ({
    labels: { colors: this.theme().mutedForeground },
  }));
  readonly chart = {
    type: 'line' as const,
    height: 280,
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
      formatter: formatReportValue,
    },
  };
  readonly tooltip = { y: { formatter: formatReportValue } };
  readonly stroke = {
    width: [3, 2, 2],
    dashArray: [0, 0, 6],
    curve: 'straight' as const,
  };
  readonly dataLabels = { enabled: false };
}
