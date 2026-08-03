import { Component, computed, inject, input } from '@angular/core';
import { Store } from '@ngrx/store';
import { BurndownPoint } from '@core/models/reporting';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import {
  formatReportValue,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { NgApexchartsModule } from 'ng-apexcharts';

// Compact counterpart of the reporting sprint burndown chart. Drops the axes, legend and
// scope series so it reads at card size.
@Component({
  selector: 'app-sprint-burndown-mini-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      i18n-aria-label="Accessible name of the sprint burndown sparkline"
      aria-label="Sprint remaining work against the ideal burndown"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis"
      [yaxis]="yaxis"
      [stroke]="stroke"
      [fill]="fill"
      [markers]="markers"
      [tooltip]="tooltip()"
      [dataLabels]="dataLabels" />
  `,
})
export class SprintBurndownMiniChartComponent {
  readonly points = input.required<BurndownPoint[]>();

  private readonly store = inject(Store);
  private readonly effectiveTheme =
    this.store.selectSignal(selectEffectiveTheme);
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => {
    const points = this.points();
    return [
      {
        name: $localize`:Label shown in the interface:Remaining`,
        data: points.map((point) => {
          return [new Date(point.date).getTime(), point.remaining];
        }),
      },
      {
        name: $localize`:Label shown in the interface:Ideal`,
        data: points.map((point) => {
          return [new Date(point.date).getTime(), point.ideal];
        }),
      },
    ];
  });

  readonly colors = computed(() => {
    return [this.theme().primary, this.theme().mutedForeground];
  });

  readonly tooltip = computed(() => {
    return {
      shared: true,
      x: { format: 'dd MMM yyyy' },
      y: { formatter: formatReportValue },
      theme: this.effectiveTheme(),
    };
  });

  readonly chart = {
    type: 'area' as const,
    height: 110,
    toolbar: { show: false },
    zoom: { enabled: false },
    sparkline: { enabled: true },
    background: 'transparent',
    animations: { enabled: false },
  };

  readonly xaxis = { type: 'datetime' as const };
  readonly yaxis = { min: 0 };

  readonly stroke = {
    width: [2, 1.5],
    dashArray: [0, 4],
    curve: 'straight' as const,
  };

  readonly fill = {
    type: ['gradient', 'gradient'],
    gradient: {
      type: 'vertical' as const,
      shadeIntensity: 0,
      opacityFrom: [0.45, 0.15],
      opacityTo: [0, 0],
      stops: [0, 100],
    },
  };

  readonly markers = { size: 0, hover: { size: 4 } };
  readonly dataLabels = { enabled: false };
}
