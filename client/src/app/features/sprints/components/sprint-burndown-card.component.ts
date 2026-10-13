import { Component, computed, inject, input } from '@angular/core';
import { BurndownPoint } from '@core/models/reporting';
import { ThemeService } from '@core/services/theme.service';
import {
  REPORT_CHART_LABEL_STYLE,
  formatReportValue,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { LucideTrendingDown } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import {
  ChartLegendComponent,
  ChartLegendItem,
} from '@static/components/chart-card/chart-legend.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { NgApexchartsModule } from 'ng-apexcharts';
import { addDays, daysBetween } from '../sprint-health';

type ChartPoint = [number, number];

const remainingLabel = $localize`:Burndown series for work left:Remaining`;
const projectedLabel = $localize`:Burndown series for the projected finish:Projected`;
const idealLabel = $localize`:Burndown series for the ideal line:Ideal`;
const scopeLabel = $localize`:Burndown series for total scope:Scope`;

@Component({
  selector: 'app-sprint-burndown-card',
  imports: [
    ChartCardComponent,
    ChartLegendComponent,
    NgApexchartsModule,
    StatStripComponent,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="burndownIcon"
      i18n-title="Heading of the sprint burndown card"
      title="Burndown"
      i18n-description="Explains what the sprint burndown chart shows"
      description="Remaining work against the ideal line">
      <app-chart-legend chartCardActions [items]="legendItems()" />

      <apx-chart
        i18n-aria-label="Accessible name of the sprint burndown chart"
        aria-label="Sprint remaining work, projection, ideal burndown and scope"
        [series]="series()"
        [chart]="chart"
        [colors]="colors()"
        [xaxis]="xaxis()"
        [yaxis]="yaxis"
        [grid]="grid()"
        [legend]="legend"
        [stroke]="stroke"
        [markers]="markers"
        [annotations]="annotations()"
        [tooltip]="tooltip()"
        [dataLabels]="dataLabels" />

      @if (stats().length) {
        <div class="-mx-6 mt-5 -mb-5">
          <app-stat-strip [items]="stats()" />
        </div>
      }
    </app-chart-card>
  `,
})
export class SprintBurndownCardComponent {
  readonly points = input.required<readonly BurndownPoint[]>();
  readonly start = input.required<Date>();
  readonly end = input.required<Date>();
  readonly today = input.required<Date>();
  readonly velocity = input<number | null>(null);
  readonly stats = input<readonly StatStripItem[]>([]);

  protected readonly burndownIcon = LucideTrendingDown;

  private readonly effectiveTheme = inject(ThemeService).theme;
  private readonly theme = reportChartThemeSignal();

  protected readonly projection = computed<ChartPoint[]>(() => {
    const latest = this.points().at(-1);
    const velocity = this.velocity();
    const daysLeft = daysBetween(this.today(), this.end());
    const canProject = latest && velocity && latest.remaining > 0;

    if (!canProject || daysLeft <= 0) return [];

    const daysToFinish = Math.min(daysLeft, latest.remaining / velocity);
    const remainingAtEnd = Math.max(
      0,
      latest.remaining - velocity * daysToFinish
    );

    return [
      [dayStamp(this.today()), latest.remaining],
      [dayStamp(addDays(this.today(), daysToFinish)), remainingAtEnd],
    ];
  });

  protected readonly legendItems = computed<ChartLegendItem[]>(() => {
    const projected: ChartLegendItem[] = this.projection().length
      ? [{ label: projectedLabel, color: 'primarySoft', line: 'dashed' }]
      : [];

    return [
      { label: remainingLabel, color: 'primary' },
      ...projected,
      { label: idealLabel, color: 'muted', line: 'dashed' },
      { label: scopeLabel, color: 'faint' },
    ];
  });

  readonly series = computed(() => {
    const points = this.points();
    const end = dayStamp(this.end());
    const startingIdeal = points[0]?.ideal ?? 0;
    const latestScope = points.at(-1)?.totalScope ?? 0;

    return [
      {
        name: remainingLabel,
        data: points.map((point): ChartPoint => [
          pointStamp(point),
          point.remaining,
        ]),
      },
      {
        name: projectedLabel,
        data: this.projection(),
      },
      {
        name: idealLabel,
        data: [
          [dayStamp(this.start()), startingIdeal],
          [end, 0],
        ],
      },
      {
        name: scopeLabel,
        data: [
          ...points.map((point): ChartPoint => [
            pointStamp(point),
            point.totalScope,
          ]),
          [end, latestScope],
        ],
      },
    ];
  });

  readonly colors = computed(() => {
    const theme = this.theme();

    return [theme.primary, theme.primary, theme.mutedForeground, theme.border];
  });

  readonly xaxis = computed(() => {
    return {
      type: 'datetime' as const,
      min: dayStamp(this.start()),
      max: dayStamp(this.end()),
      labels: { style: REPORT_CHART_LABEL_STYLE, datetimeUTC: true },
      axisBorder: { show: false },
      axisTicks: { show: false },
      tooltip: { enabled: false },
    };
  });

  readonly grid = computed(() => {
    return {
      borderColor: this.theme().border,
      strokeDashArray: 0,
      xaxis: { lines: { show: false } },
      padding: { left: 4, right: 8 },
    };
  });

  readonly annotations = computed(() => {
    return {
      xaxis: [
        {
          x: dayStamp(this.today()),
          borderColor: this.theme().mutedForeground,
          strokeDashArray: 0,
        },
      ],
    };
  });

  readonly tooltip = computed(() => {
    return {
      shared: true,
      x: { format: 'dd MMM' },
      y: { formatter: formatReportValue },
      theme: this.effectiveTheme(),
    };
  });

  readonly chart = {
    type: 'line' as const,
    height: 220,
    toolbar: { show: false },
    zoom: { enabled: false },
    background: 'transparent',
    animations: { enabled: false },
  };

  readonly yaxis = {
    min: 0,
    forceNiceScale: true,
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      formatter: formatReportValue,
    },
  };

  readonly legend = { show: false };

  readonly stroke = {
    width: [2.5, 2, 1, 2],
    dashArray: [0, 5, 4, 0],
    curve: 'straight' as const,
  };

  readonly markers = { size: 0, hover: { size: 4 } };
  readonly dataLabels = { enabled: false };
}

// Burndown dates are local calendar days; plotting them at UTC midnight keeps every
// series on the same day grid without the viewer's offset shifting them.
function dayStamp(date: Date): number {
  return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
}

function pointStamp(point: BurndownPoint): number {
  return new Date(point.date).getTime();
}
