import { Component, computed, inject } from '@angular/core';
import { LucideTimer } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { DashboardFlowService } from '../services/dashboard-flow.service';
import { CycleTimeChartComponent } from './charts/cycle-time-chart.component';

function formatHours(value: number | null | undefined): string {
  if (value == null) return '—';

  if (value >= 48) {
    return `${Math.round(value / 24)}d`;
  }

  return `${Math.round(value * 10) / 10}h`;
}

@Component({
  selector: 'app-dashboard-cycle-time-card',
  imports: [
    ChartCardComponent,
    CycleTimeChartComponent,
    EmptyStateComponent,
    SkeletonComponent,
    StatStripComponent,
    LucideTimer,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="cycleTimeIcon"
      i18n-title="Heading of the dashboard cycle time card"
      title="Cycle time"
      i18n-description="Explains what the cycle time chart measures"
      description="How long finished tasks took, by week">
      @if (flow.isInitialLoad()) {
        <div
          role="status"
          i18n-aria-label="Accessible label while the cycle time chart loads"
          aria-label="Loading cycle time">
          <app-skeleton class="h-52 w-full" />
          <app-skeleton class="mt-6 h-10 w-full" />
        </div>
      } @else if (hasData()) {
        <app-cycle-time-chart [buckets]="buckets()" />
        <div class="-mx-6 mt-5 -mb-5">
          <app-stat-strip [items]="stats()" />
        </div>
      } @else {
        <app-empty-state
          compact
          i18n-title="Empty state for the dashboard cycle time card"
          title="Not enough finished work yet."
          i18n-description="Explains why the cycle time chart is empty"
          description="Cycle time appears once tasks have been completed.">
          <svg emptyStateIcon lucideTimer class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class DashboardCycleTimeCardComponent {
  protected readonly flow = inject(DashboardFlowService);
  protected readonly cycleTimeIcon = LucideTimer;

  protected readonly buckets = computed(
    () => this.flow.report()?.cycleTimeBuckets ?? []
  );

  protected readonly hasData = computed(() => {
    const measured = this.buckets().filter(
      (bucket) => bucket.medianCycleTimeHours != null
    );

    return measured.length > 1;
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const report = this.flow.report();

    if (!report) return [];

    return [
      {
        label: $localize`:Label for the middle cycle time value:Median`,
        value: formatHours(report.medianCycleTimeHours),
      },
      {
        label: $localize`:Label for the 85th percentile cycle time:85th percentile`,
        value: formatHours(report.p85CycleTimeHours),
      },
      {
        label: $localize`:Label for how many tasks the cycle time was measured from:Measured from`,
        value: report.cycleTimeSampleSize,
      },
    ];
  });
}
