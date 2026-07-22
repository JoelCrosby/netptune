import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskStatusBreakdown } from '@core/models/view-models/task-status-breakdown';
import { NamedColor } from '@core/util/colors/colors';
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
      color: status.color ?? fallbackPalette[index % fallbackPalette.length],
    }))
  );

  readonly statusTotal = computed(
    () => this.breakdown.value()?.payload?.total ?? 0
  );
}
