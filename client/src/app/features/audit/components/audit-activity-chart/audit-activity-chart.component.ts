import { Component, computed, inject } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';
import { AuditFilterService } from '@audit/audit-filter.service';
import { auditSummaryResource } from '@core/resources/audit.resource';
import {
  REPORT_CHART_LABEL_STYLE,
  reportChartThemeSignal,
} from '@core/util/chart-theme';
import { LucideActivity } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { NgApexchartsModule } from 'ng-apexcharts';

@Component({
  selector: 'app-audit-activity-chart',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    LucideActivity,
    NgApexchartsModule,
    SkeletonComponent,
  ],
  host: { class: 'block' },
  template: `
    <app-chart-card
      [icon]="activityIcon"
      i18n-title="Heading of the audit activity chart"
      title="Activity over time"
      i18n-description="Explains what the audit activity chart plots"
      description="Recorded events per day">
      @if (summary.isLoading()) {
        <div
          role="status"
          i18n-aria-label="Shown while the audit activity chart loads"
          aria-label="Loading activity">
          <app-skeleton class="h-45 w-full" />
        </div>
      } @else if (hasData()) {
        <apx-chart
          i18n-aria-label="Accessible name of the audit activity chart"
          aria-label="Recorded audit events per day"
          [series]="series()"
          [chart]="chartConfig"
          [colors]="colors()"
          [xaxis]="xaxis()"
          [yaxis]="yaxis"
          [stroke]="stroke"
          [fill]="fill"
          [dataLabels]="dataLabels"
          [grid]="grid()"
          [tooltip]="tooltip()" />
      } @else {
        <app-empty-state
          compact
          i18n-title="Empty state for the audit activity chart"
          title="No activity in this period."
          i18n-description="Advice shown when the audit period has no events"
          description="Widen the date range to see recorded events.">
          <svg emptyStateIcon lucideActivity class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class AuditActivityChartComponent {
  private readonly filters = inject(AuditFilterService);

  protected readonly summary = auditSummaryResource(this.filters.filter);

  private readonly effectiveTheme = inject(ThemeService).theme;
  private readonly theme = reportChartThemeSignal();

  protected readonly activityIcon = LucideActivity;

  protected readonly hasData = computed(() =>
    this.summary.value().some((point) => point.count > 0)
  );

  readonly series = computed(() => [
    {
      name: $localize`:Series name for recorded audit events:Events`,
      data: this.summary.value().map((point) => {
        return [new Date(point.date).getTime(), point.count];
      }),
    },
  ]);

  readonly colors = computed(() => [this.theme().primary]);

  readonly grid = computed(() => ({
    borderColor: this.theme().border,
    strokeDashArray: 4,
    xaxis: { lines: { show: false } },
    padding: { left: 0, right: 0 },
  }));

  readonly xaxis = computed(() => {
    const points = this.summary.value();
    const last = points[points.length - 1];

    return {
      type: 'datetime' as const,
      min: points[0] ? new Date(points[0].date).getTime() : undefined,
      max: last ? new Date(last.date).getTime() : undefined,
      labels: { style: REPORT_CHART_LABEL_STYLE, datetimeUTC: false },
      axisBorder: { show: false },
      axisTicks: { show: false },
    };
  });

  readonly chartConfig = {
    type: 'area' as const,
    height: 180,
    toolbar: { show: false },
    zoom: { enabled: false },
    animations: { enabled: false },
    background: 'transparent',
  };

  readonly yaxis = {
    min: 0,
    tickAmount: 4,
    labels: {
      style: REPORT_CHART_LABEL_STYLE,
      formatter: (value: number) => Math.floor(value).toString(),
    },
  };

  readonly stroke = { curve: 'smooth' as const, width: 2 };

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

  readonly dataLabels = { enabled: false };

  readonly tooltip = computed(() => ({
    x: { format: 'dd MMM yyyy' },
    theme: this.effectiveTheme(),
  }));
}
