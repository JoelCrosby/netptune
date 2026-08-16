import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { WorkloadReport } from '@core/models/reporting';
import { LucideUsers } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { WorkloadChartComponent } from './charts/workload-chart.component';

const topAssignees = 8;

@Component({
  selector: 'app-dashboard-workload-card',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    SkeletonComponent,
    StatStripComponent,
    WorkloadChartComponent,
    LucideUsers,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="workloadIcon"
      i18n-title="Heading of the dashboard workload card"
      title="Team workload"
      i18n-description="Explains what the workload chart shows"
      description="Open tasks by assignee">
      @if (isInitialLoad()) {
        <div
          role="status"
          i18n-aria-label="Accessible label while the workload chart loads"
          aria-label="Loading team workload">
          <app-skeleton class="h-52 w-full" />
          <app-skeleton class="mt-6 h-10 w-full" />
        </div>
      } @else if (rows().length) {
        <app-workload-chart [rows]="rows()" />
        <div class="-mx-6 mt-5 -mb-5">
          <app-stat-strip [items]="stats()" />
        </div>
      } @else {
        <app-empty-state
          compact
          i18n-title="Empty state for the dashboard workload card"
          title="No assigned work open."
          i18n-description="Advice shown when nobody has open assigned tasks"
          description="Assign tasks to see how work is spread across the team.">
          <svg emptyStateIcon lucideUsers class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class DashboardWorkloadCardComponent {
  protected readonly workloadIcon = LucideUsers;

  readonly canRead = hasPermission(PERMISSIONS.members.read);

  private readonly resource = httpResource<WorkloadReport>(() => {
    return this.canRead()
      ? { url: 'api/reports/workload', params: { unit: 'Tasks' } }
      : undefined;
  });

  protected readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  protected readonly rows = computed(() => {
    const rows = this.resource.value()?.rows ?? [];

    return [...rows]
      .sort((left, right) => right.taskCount - left.taskCount)
      .slice(0, topAssignees)
      .reverse();
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const report = this.resource.value();

    if (!report) return [];

    return [
      {
        label: $localize`:Label for the number of open assigned tasks:Assigned`,
        value: report.uniqueTaskCount,
      },
      {
        label: $localize`:Label for the number of open tasks with no assignee:Unassigned`,
        value: report.unassignedTaskCount,
      },
      {
        label: $localize`:Label for tasks assigned to more than one person:Shared`,
        value: report.multiAssignedTaskCount,
      },
    ];
  });
}
