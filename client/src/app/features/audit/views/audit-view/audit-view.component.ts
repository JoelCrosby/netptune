import { Component } from '@angular/core';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
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
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    AuditFiltersComponent,
    AuditActivityChartComponent,
    AuditTableComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the workspace audit log"
        title="Audit Log"
        i18n-filtersLabel="Accessible name of the audit log filter row"
        filtersLabel="Filter the audit log">
        <app-audit-filters
          pageHeaderFilters
          (filterChange)="auditTable.goToFirstPage()" />
      </app-page-header>

      <app-page-body scroll>
        <div class="flex flex-col gap-6 pb-4">
          <app-audit-activity-chart />
          <app-audit-table #auditTable />
        </div>
      </app-page-body>
    </app-page-container>
  `,
})
export class AuditViewComponent {}
