import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskStatusBreakdown } from '@core/models/view-models/task-status-breakdown';
import {
  DonutStatCardComponent,
  DonutStatItem,
} from '@static/components/donut-stat-card/donut-stat-card.component';

// Distinct fallback hues for statuses that have no colour configured. Indexed
// by position so two statuses (even in the same category) never collapse to the
// same slice colour the way a category-keyed palette would.
const fallbackPalette = [
  '#3b82f6',
  '#22c55e',
  '#eab308',
  '#a855f7',
  '#ec4899',
  '#06b6d4',
  '#f97316',
  '#14b8a6',
  '#6366f1',
  '#ef4444',
];

@Component({
  selector: 'app-dashboard-status-breakdown-card',
  imports: [DonutStatCardComponent],
  template: `
    <app-donut-stat-card
      title="Tasks by status"
      totalLabel="Total"
      emptyMessage="No tasks to display."
      [items]="statusItems()"
      [total]="statusTotal()" />
  `,
})
export class DashboardStatusBreakdownCardComponent {
  private readonly breakdown = httpResource<
    ClientResponse<TaskStatusBreakdown>
  >(() => 'api/tasks/status-breakdown');

  readonly statusItems = computed<DonutStatItem[]>(() =>
    (this.breakdown.value()?.payload?.statuses ?? []).map((status, index) => ({
      label: status.name,
      value: status.count,
      color:
        status.color?.trim() || fallbackPalette[index % fallbackPalette.length],
    }))
  );

  readonly statusTotal = computed(
    () => this.breakdown.value()?.payload?.total ?? 0
  );
}
