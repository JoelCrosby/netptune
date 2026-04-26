import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { AuditFiltersComponent } from '@audit/components/audit-filters/audit-filters.component';
import { AuditTableComponent } from '@audit/components/audit-table/audit-table.component';
import { AuditStore } from '@audit/audit-state.service';

@Component({
  selector: 'app-audit-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AuditStore],
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    AuditFiltersComponent,
    AuditTableComponent,
  ],
  template: `
    <app-page-container>
      <app-page-header title="Audit Log" />
      <app-audit-filters />
      <app-audit-table />
    </app-page-container>
  `,
})
export class AuditViewComponent {}
