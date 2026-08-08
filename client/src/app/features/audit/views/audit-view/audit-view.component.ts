import { Component } from '@angular/core';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { AuditFiltersComponent } from '@audit/components/audit-filters/audit-filters.component';
import { AuditTableComponent } from '@audit/components/audit-table/audit-table.component';
import { AuditActivityChartComponent } from '@audit/components/audit-activity-chart/audit-activity-chart.component';
import { AuditFilterService } from '@audit/audit-filter.service';

@Component({
  selector: 'app-audit-view',
  providers: [AuditFilterService],
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    AuditFiltersComponent,
    AuditActivityChartComponent,
    AuditTableComponent,
  ],
  template: `
    <app-page-container>
      <app-page-header
        i18n-title="Page title for the workspace audit log"
        title="Audit Log" />
      <div class="flex flex-col gap-6">
        <app-audit-filters (filterChange)="auditTable.goToFirstPage()" />
        <app-audit-activity-chart />
        <app-audit-table #auditTable />
      </div>
    </app-page-container>
  `,
})
export class AuditViewComponent {}
