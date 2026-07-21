import { Component, inject, output, signal } from '@angular/core';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';
import { AuditService } from '@core/store/audit/audit.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AuditStore } from '@audit/audit-state.service';
import { downloadFile } from '@core/util/download-helper';
import { AuditDateFilterComponent } from './audit-date-filter.component';

@Component({
  selector: 'app-audit-filters',
  imports: [
    AuditDateFilterComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div class="mb-8 flex flex-wrap items-end gap-3">
      <app-audit-date-filter
        controlId="from-date"
        label="From"
        [(value)]="fromDate" />

      <app-audit-date-filter
        controlId="to-date"
        label="To"
        [(value)]="toDate" />

      <button app-stroked-button (click)="onApply()">Filter</button>
      <button app-stroked-button (click)="onReset()">Reset</button>

      <button app-flat-button (click)="onExport()" class="ml-auto">
        Export CSV
      </button>
    </div>
  `,
})
export class AuditFiltersComponent {
  private state = inject(AuditStore);
  private auditService = inject(AuditService);

  fromDate = signal<string>('');
  toDate = signal<string>('');
  readonly filterChange = output();

  onApply() {
    this.state.applyFilters(
      this.fromDate() || undefined,
      this.toDate() || undefined
    );
    this.filterChange.emit();
  }

  onReset() {
    this.fromDate.set('');
    this.toDate.set('');
    this.state.reset();
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
