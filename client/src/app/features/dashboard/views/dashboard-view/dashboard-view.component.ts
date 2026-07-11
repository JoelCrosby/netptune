import { Component } from '@angular/core';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { DashboardCurrentSprintCardComponent } from '../../components/dashboard-current-sprint-card.component';
import { DashboardStatusBreakdownCardComponent } from '../../components/dashboard-status-breakdown-card.component';
import { DashboardNotificationsCardComponent } from '../../components/dashboard-notifications-card.component';
import { DashboardAssignedTasksComponent } from '../../components/dashboard-assigned-tasks.component';

@Component({
  selector: 'app-dashboard-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    DashboardCurrentSprintCardComponent,
    DashboardStatusBreakdownCardComponent,
    DashboardNotificationsCardComponent,
    DashboardAssignedTasksComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Dashboard" />

      <div class="flex flex-col gap-8">
        <app-dashboard-current-sprint-card />

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <app-dashboard-status-breakdown-card />
          <app-dashboard-notifications-card class="lg:relative" />
        </div>

        <app-dashboard-assigned-tasks />
      </div>
    </app-page-container>
  `,
})
export class DashboardViewComponent {}
