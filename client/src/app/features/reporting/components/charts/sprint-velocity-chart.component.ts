import { Component, computed, input } from '@angular/core';
import { VelocityPoint } from '@core/models/reporting';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '../../utils/report-chart-theme';

@Component({
  selector: 'app-sprint-velocity-chart',
  imports: [NgApexchartsModule],
  host: { class: 'block' },
  template: `
    <apx-chart
      aria-label="Committed and completed sprint velocity"
      [series]="series()"
      [chart]="chart"
      [colors]="colors()"
      [xaxis]="xaxis()"
      [yaxis]="yaxis"
      [grid]="grid()"
      [legend]="legend()"
      [dataLabels]="dataLabels" />
  `,
})
export class SprintVelocityChartComponent {
  readonly sprints = input.required<VelocityPoint[]>();
  private readonly theme = reportChartThemeSignal();

  readonly series = computed(() => [
    {
      name: 'Committed',
      data: this.sprints().map((point) => point.committed),
    },
    {
      name: 'Completed',
      data: this.sprints().map((point) => point.completed),
    },
  ]);
  readonly colors = computed(() => [
    this.theme().primary,
    this.theme().mutedForeground,
  ]);
  readonly xaxis = computed(() => ({
    categories: this.sprints().map((point) => point.sprintName),
    labels: { style: REPORT_CHART_LABEL_STYLE },
  }));
  readonly grid = computed(() => ({ borderColor: this.theme().border }));
  readonly legend = computed(() => ({
    labels: { colors: this.theme().mutedForeground },
  }));
  readonly chart = {
    type: 'bar' as const,
    height: 260,
    toolbar: { show: false },
  };
  readonly yaxis = {
    min: 0,
    labels: { style: REPORT_CHART_LABEL_STYLE },
  };
  readonly dataLabels = { enabled: false };
}
