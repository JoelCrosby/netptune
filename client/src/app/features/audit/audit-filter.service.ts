import { Injectable, signal } from '@angular/core';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';

@Injectable()
export class AuditFilterService {
  private readonly current = signal<AuditLogFilter>({});

  readonly filter = this.current.asReadonly();

  apply(from?: string, to?: string) {
    this.current.set({ from, to });
  }

  reset() {
    this.current.set({});
  }
}
