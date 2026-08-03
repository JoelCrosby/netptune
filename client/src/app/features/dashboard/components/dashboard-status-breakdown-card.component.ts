import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskStatusBreakdown } from '@core/models/view-models/task-status-breakdown';
import { NamedColor } from '@core/util/colors/colors';
import { LucideChartPie } from '@lucide/angular';
import {
  DonutStatCardComponent,
  DonutStatItem,
} from '@static/components/donut-stat-card/donut-stat-card.component';

const fallbackPalette: NamedColor[] = [
  'blue',
  'green',
  'yellow',
  'purple',
  'pink',
  'cyan',
  'orange',
  'teal',
  'indigo',
  'red',
];

@Component({
  selector: 'app-dashboard-status-breakdown-card',
  imports: [DonutStatCardComponent],
  template: `
    <app-donut-stat-card
      i18n-title="Heading of the dashboard status breakdown card"
      title="Tasks by status"
      i18n-totalLabel="Centre label showing the total task count"
      totalLabel="Total"
      i18n-emptyMessage="Empty state for the status breakdown card"
      emptyMessage="No tasks to display."
      [icon]="breakdownIcon"
      [items]="statusItems()"
      [total]="statusTotal()"
      [loading]="isInitialLoad()" />
  `,
})
export class DashboardStatusBreakdownCardComponent {
  private readonly breakdown = httpResource<
    ClientResponse<TaskStatusBreakdown>
  >(() => 'api/tasks/status-breakdown');

  protected readonly breakdownIcon = LucideChartPie;

  readonly statusItems = computed<DonutStatItem[]>(() =>
    (this.breakdown.value()?.payload?.statuses ?? []).map((status, index) => ({
      label: status.name,
      value: status.count,
      color: status.color ?? fallbackPalette[index % fallbackPalette.length],
    }))
  );

  readonly statusTotal = computed(
    () => this.breakdown.value()?.payload?.total ?? 0
  );

  readonly isInitialLoad = computed(
    () => this.breakdown.isLoading() && !this.breakdown.hasValue()
  );
}
