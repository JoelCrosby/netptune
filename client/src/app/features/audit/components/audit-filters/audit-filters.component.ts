import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';
import { AuditService } from '@core/store/audit/audit.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AuditStore } from '@audit/audit-state.service';
import { downloadFile } from '@core/util/download-helper';

@Component({
  selector: 'app-audit-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, FlatButtonComponent, StrokedButtonComponent],
  template: `
    <div class="mb-8 flex flex-wrap items-end gap-3">
      <div class="flex flex-col gap-1">
        <label
          for="from-date"
          class="text-foreground/60 text-xs font-medium tracking-wide uppercase">
          From
        </label>
        <input
          id="from-date"
          type="date"
          [(ngModel)]="fromDate"
          class="bg-background border-border h-10 rounded-sm border px-3 py-1.5 text-sm" />
      </div>

      <div class="flex flex-col gap-1">
        <label
          for="to-date"
          class="text-foreground/60 text-xs font-medium tracking-wide uppercase">
          To
        </label>
        <input
          id="to-date"
          type="date"
          [(ngModel)]="toDate"
          class="bg-background border-border h-10 rounded-sm border px-3 py-1.5 text-sm" />
      </div>

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

  onApply() {
    this.state.applyFilters(
      this.fromDate() || undefined,
      this.toDate() || undefined
    );
  }

  onReset() {
    this.fromDate.set('');
    this.toDate.set('');
    this.state.reset();
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
