import { Component, computed, inject, input } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';
import { WorkloadRow } from '@core/models/reporting';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { NgApexchartsModule } from 'ng-apexcharts';

/** Matches the other dashboard charts so cards in a row stay the same height. */
const chartHeight = 210;
const maxBarPixels = 18;
const maxBarPercent = 62;

@Component({
  selector: 'app-workload-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      i18n-aria-label="Accessible name of the dashboard workload chart"
      aria-label="Open tasks per assignee"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis()"
      [yaxis]="yaxis"
      [grid]="grid()"
      [plotOptions]="plotOptions()"
      [tooltip]="tooltip()"
      [legend]="legend"
      [dataLabels]="dataLabels()" />
  `,
})
export class WorkloadChartComponent {
  readonly rows = input.required<readonly WorkloadRow[]>();

  private readonly effectiveTheme = inject(ThemeService).theme;
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: $localize`:Series name for the number of open tasks:Open tasks`,
      data: this.rows().map((row) => row.taskCount),
    },
  ]);

  readonly colors = computed(() => [this.theme().primary]);

  readonly chart = {
    type: 'bar' as const,
    height: chartHeight,
    toolbar: { show: false },
    background: 'transparent',
    animations: { enabled: false },
  };

  readonly xaxis = computed(() => ({
    categories: this.rows().map((row) => row.displayName),
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      formatter: (value: string) => Math.round(Number(value)).toString(),
    },
    axisBorder: { show: false },
    axisTicks: { show: false },
  }));

  readonly grid = computed(() => ({
    borderColor: this.theme().border,
    strokeDashArray: 4,
    padding: { left: 0, right: 0 },
  }));

  readonly tooltip = computed(() => ({ theme: this.effectiveTheme() }));

  readonly dataLabels = computed(() => ({
    enabled: true,
    offsetX: 20,
    style: { fontSize: '11px', colors: [this.theme().mutedForeground] },
  }));

  readonly yaxis = { labels: { style: REPORT_CHART_LABEL_STYLE } };

  /**
   * The chart height is fixed, so a short list would otherwise render one huge
   * bar per row. Thickness is capped in pixels and expressed back as the
   * percentage ApexCharts expects.
   */
  readonly plotOptions = computed(() => {
    const rows = Math.max(1, this.rows().length);
    const rowPixels = chartHeight / rows;
    const share = Math.min(maxBarPercent, (maxBarPixels / rowPixels) * 100);

    return {
      bar: {
        horizontal: true,
        barHeight: `${Math.round(share)}%`,
        borderRadius: 3,
        borderRadiusApplication: 'end' as const,
        dataLabels: { position: 'top' as const },
      },
    };
  });

  readonly legend = { show: false };
}
