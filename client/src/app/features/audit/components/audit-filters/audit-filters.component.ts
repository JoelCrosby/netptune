import { Component, inject, output, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';
import { AuditService } from '@core/services/audit.service';
import { LucideDownload } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AuditFilterService } from '@audit/audit-filter.service';
import { downloadFile } from '@core/util/download-helper';
import { AuditDateFilterComponent } from './audit-date-filter.component';

@Component({
  selector: 'app-audit-filters',
  imports: [
    AuditDateFilterComponent,
    FlatButtonComponent,
    LucideDownload,
    StrokedButtonComponent,
  ],
  template: `
    <div
      class="border-border bg-card flex flex-wrap items-end gap-3 rounded-lg border px-6 py-4 shadow-sm">
      <app-audit-date-filter
        controlId="from-date"
        i18n-label="Label of the start-date filter"
        label="From"
        [(value)]="fromDate" />

      <app-audit-date-filter
        controlId="to-date"
        i18n-label="Label of the end-date filter"
        label="To"
        [(value)]="toDate" />

      <button app-stroked-button type="button" (click)="onApply()">
        <span i18n="Button that applies the filters">Filter</span>
      </button>
      <button app-stroked-button type="button" (click)="onReset()">
        <span i18n="Button that clears the filters">Reset</span>
      </button>

      @if (canExport()) {
        <button
          app-flat-button
          type="button"
          class="ml-auto gap-2"
          (click)="onExport()">
          <svg lucideDownload class="h-4 w-4"></svg>
          <span i18n="Button that downloads the audit log as CSV">
            Export CSV
          </span>
        </button>
      }
    </div>
  `,
})
export class AuditFiltersComponent {
  private filters = inject(AuditFilterService);
  private auditService = inject(AuditService);

  protected readonly canExport = hasPermission(PERMISSIONS.audit.export);

  fromDate = signal<string>('');
  toDate = signal<string>('');
  readonly filterChange = output();

  onApply() {
    this.filters.apply(
      this.fromDate() || undefined,
      this.toDate() || undefined
    );
    this.filterChange.emit();
  }

  onReset() {
    this.fromDate.set('');
    this.toDate.set('');
    this.filters.reset();
    this.filterChange.emit();
  }

  onExport() {
    const filter: AuditLogFilter = {
      from: this.fromDate() || undefined,
      to: this.toDate() || undefined,
    };

    this.auditService.exportAuditLog(filter).subscribe((response) => {
      const cd = response.headers.get('content-disposition') ?? '';
      const blob = response.body;

      if (!blob) return;

      const filename =
        cd.match(/filename="?([^"]+)"?/)?.[1] ?? 'netptune-audit-export.csv';

      downloadFile(blob, filename);
    });
  }
}
