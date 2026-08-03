import { Component, computed, inject } from '@angular/core';
import { LucideTrendingUp } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { DashboardFlowService } from '../services/dashboard-flow.service';
import { ThroughputChartComponent } from './charts/throughput-chart.component';

@Component({
  selector: 'app-dashboard-throughput-card',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    SkeletonComponent,
    StatStripComponent,
    ThroughputChartComponent,
    LucideTrendingUp,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="throughputIcon"
      i18n-title="Heading of the dashboard throughput card"
      title="Throughput"
      [description]="cardDescription()">
      @if (flow.isInitialLoad()) {
        <div
          role="status"
          i18n-aria-label="Accessible label while the throughput chart loads"
          aria-label="Loading throughput">
          <app-skeleton class="h-52 w-full" />
          <app-skeleton class="mt-6 h-10 w-full" />
        </div>
      } @else if (hasData()) {
        <app-throughput-chart [buckets]="buckets()" />
        <div class="-mx-6 mt-5 -mb-5">
          <app-stat-strip [items]="stats()" />
        </div>
      } @else {
        <app-empty-state
          compact
          i18n-title="Empty state for the dashboard throughput card"
          title="No completed tasks yet."
          i18n-description="Advice shown when nothing has been completed"
          description="Completed tasks will show up here as they land.">
          <svg emptyStateIcon lucideTrendingUp class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class DashboardThroughputCardComponent {
  protected readonly flow = inject(DashboardFlowService);
  protected readonly throughputIcon = LucideTrendingUp;

  protected readonly buckets = computed(
    () => this.flow.report()?.buckets ?? []
  );

  protected readonly hasData = computed(() =>
    this.buckets().some((bucket) => bucket.completed > 0)
  );

  protected readonly cardDescription = computed(() => {
    return $localize`:Period covered by the dashboard throughput chart:Last ${this.flow.trailingDays}:DAYS: days`;
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const report = this.flow.report();

    if (!report) return [];

    const buckets = this.buckets();
    const busiest = buckets.reduce(
      (highest, bucket) => Math.max(highest, bucket.completed),
      0
    );
    const perDay = buckets.length ? report.throughput / buckets.length : 0;

    return [
      {
        label: $localize`:Label for the number of tasks completed in the period:Completed`,
        value: report.throughput,
      },
      {
        label: $localize`:Label for the mean number of tasks completed each day:Daily average`,
        value: Math.round(perDay * 10) / 10,
      },
      {
        label: $localize`:Label for the number of tasks still open:Still open`,
        value: report.currentOpenTaskCount,
      },
      {
        label: $localize`:Label for the highest number of tasks completed in one day:Best day`,
        value: busiest,
      },
    ];
  });
}
