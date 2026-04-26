import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { NgApexchartsModule } from 'ng-apexcharts';
import { AuditStore } from '@audit/audit-state.service';

@Component({
  selector: 'app-audit-activity-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgApexchartsModule],
  template: `
    <div class="border-border mb-6 rounded border p-4">
      <p
        class="text-foreground/60 mb-3 text-xs font-medium tracking-wide uppercase">
        Activity Over Time
      </p>
      <apx-chart
        [series]="series()"
        [chart]="chartConfig"
        [xaxis]="xaxis()"
        [yaxis]="yaxis"
        [stroke]="stroke"
        [fill]="fill"
        [dataLabels]="dataLabels"
        [grid]="grid"
        [tooltip]="tooltip" />
    </div>
  `,
})
export class AuditActivityChartComponent {
  private store = inject(AuditStore);

  series = computed(() => [
    {
      name: 'Events',
      data: this.store
        .summary()
        .map((p) => [new Date(p.date).getTime(), p.count]),
    },
  ]);

  xaxis = computed(() => {
    const points = this.store.summary();
    return {
      type: 'datetime' as const,
      min: points[0] ? new Date(points[0].date).getTime() : undefined,
      max: points[points.length - 1]
        ? new Date(points[points.length - 1].date).getTime()
        : undefined,
      labels: {
        style: { colors: 'hsl(var(--foreground) / 0.5)', fontSize: '11px' },
        datetimeUTC: false,
      },
      axisBorder: { show: false },
      axisTicks: { show: false },
    };
  });

  readonly chartConfig = {
    type: 'area' as const,
    height: 180,
    toolbar: { show: false },
    zoom: { enabled: false },
    sparkline: { enabled: false },
    animations: { enabled: false },
    background: 'transparent',
  };

  readonly yaxis = {
    min: 0,
    tickAmount: 4,
    labels: {
      style: { colors: 'hsl(var(--foreground) / 0.5)', fontSize: '11px' },
      formatter: (v: number) => Math.floor(v).toString(),
    },
  };

  readonly stroke = { curve: 'smooth' as const, width: 2 };

  readonly fill = {
    type: 'gradient',
    gradient: {
      shadeIntensity: 1,
      opacityFrom: 0.35,
      opacityTo: 0.02,
      stops: [0, 100],
    },
  };

  readonly dataLabels = { enabled: false };

  readonly grid = {
    borderColor: 'hsl(var(--border))',
    strokeDashArray: 4,
    xaxis: { lines: { show: false } },
  };

  readonly tooltip = {
    x: { format: 'dd MMM yyyy' },
    theme: 'dark',
  };
}
