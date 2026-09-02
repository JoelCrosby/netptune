import { Component, computed } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { DashboardAssignedTasksComponent } from '../../components/dashboard-assigned-tasks.component';
import { DashboardCurrentSprintCardComponent } from '../../components/dashboard-current-sprint-card.component';
import { DashboardCycleTimeCardComponent } from '../../components/dashboard-cycle-time-card.component';
import { DashboardNotificationsCardComponent } from '../../components/dashboard-notifications-card.component';
import { DashboardPinnedCardComponent } from '../../components/dashboard-pinned-card.component';
import { DashboardStatusBreakdownCardComponent } from '../../components/dashboard-status-breakdown-card.component';
import { DashboardThroughputCardComponent } from '../../components/dashboard-throughput-card.component';
import { DashboardVelocityCardComponent } from '../../components/dashboard-velocity-card.component';
import { DashboardWorkloadCardComponent } from '../../components/dashboard-workload-card.component';
import { DashboardFlowService } from '../../services/dashboard-flow.service';

@Component({
  selector: 'app-dashboard-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    DashboardAssignedTasksComponent,
    DashboardCurrentSprintCardComponent,
    DashboardCycleTimeCardComponent,
    DashboardNotificationsCardComponent,
    DashboardPinnedCardComponent,
    DashboardStatusBreakdownCardComponent,
    DashboardThroughputCardComponent,
    DashboardVelocityCardComponent,
    DashboardWorkloadCardComponent,
  ],
  providers: [DashboardFlowService],
  template: `
    <app-page-container
      followsWidthPreference
      [centerPage]="true"
      [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the dashboard"
        title="Dashboard" />

      <div class="flex flex-col gap-8">
        <app-dashboard-pinned-card />

        <app-dashboard-current-sprint-card />

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <app-dashboard-throughput-card />
          <app-dashboard-cycle-time-card />
        </div>

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <app-dashboard-status-breakdown-card />
          <app-dashboard-notifications-card class="lg:relative" />
        </div>

        @if (canSeeVelocity() || canSeeWorkload()) {
          <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
            @if (canSeeVelocity()) {
              <app-dashboard-velocity-card [class]="velocitySpanClass()" />
            }
            @if (canSeeWorkload()) {
              <app-dashboard-workload-card [class]="workloadSpanClass()" />
            }
          </div>
        }

        <app-dashboard-assigned-tasks />
      </div>
    </app-page-container>
  `,
})
export class DashboardViewComponent {
  protected readonly canSeeWorkload = hasPermission(PERMISSIONS.members.read);

  protected readonly canSeeVelocity = hasPermission(PERMISSIONS.sprints.read);

  protected readonly velocitySpanClass = computed(() =>
    this.canSeeWorkload() ? '' : 'lg:col-span-2'
  );

  protected readonly workloadSpanClass = computed(() =>
    this.canSeeVelocity() ? '' : 'lg:col-span-2'
  );
}
