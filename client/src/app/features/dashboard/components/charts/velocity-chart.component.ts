import { Component, computed, inject, input } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';
import { VelocityPoint } from '@core/models/reporting';
import {
  REPORT_CHART_LABEL_STYLE,
  formatReportValue,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { NgApexchartsModule } from 'ng-apexcharts';

@Component({
  selector: 'app-velocity-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      i18n-aria-label="Accessible name of the dashboard velocity chart"
      aria-label="Committed and completed work per sprint"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis()"
      [yaxis]="yaxis"
      [grid]="grid()"
      [plotOptions]="plotOptions"
      [stroke]="stroke"
      [legend]="legend()"
      [tooltip]="tooltip()"
      [dataLabels]="dataLabels" />
  `,
})
export class VelocityChartComponent {
  readonly sprints = input.required<readonly VelocityPoint[]>();

  private readonly effectiveTheme = inject(ThemeService).theme;
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: $localize`:Series name for work a sprint took on:Committed`,
      data: this.sprints().map((point) => point.committed),
    },
    {
      name: $localize`:Series name for work a sprint finished:Completed`,
      data: this.sprints().map((point) => point.completed),
    },
  ]);

  readonly colors = computed(() => [
    this.theme().mutedForeground,
    this.theme().primary,
  ]);

  readonly xaxis = computed(() => ({
    categories: this.sprints().map((point) => point.sprintName),
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      trim: true,
      hideOverlappingLabels: true,
    },
    axisBorder: { show: false },
    axisTicks: { show: false },
  }));

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
    intersect: false,
    y: { formatter: formatReportValue },
    theme: this.effectiveTheme(),
  }));

  readonly chart = {
    type: 'bar' as const,
    height: 210,
    toolbar: { show: false },
    background: 'transparent',
    animations: { enabled: false },
  };

  readonly plotOptions = {
    bar: {
      columnWidth: '58%',
      borderRadius: 3,
      borderRadiusApplication: 'end' as const,
    },
  };

  readonly yaxis = {
    min: 0,
    forceNiceScale: true,
    labels: { style: REPORT_CHART_LABEL_STYLE, formatter: formatReportValue },
  };

  readonly stroke = { show: true, width: 2, colors: ['transparent'] };
  readonly dataLabels = { enabled: false };
}
