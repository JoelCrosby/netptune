import { computed, Injectable, signal } from '@angular/core';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';

@Injectable()
export class AuditFilterService {
  private readonly current = signal<AuditLogFilter>({});

  readonly filter = this.current.asReadonly();

  readonly hasFilters = computed(() => {
    const filter = this.current();

    return (
      !!filter.userId ||
      filter.entityType !== undefined ||
      filter.activityType !== undefined ||
      !!filter.from ||
      !!filter.to
    );
  });

  update(patch: AuditLogFilter) {
    this.current.update((current) => ({ ...current, ...patch }));
  }

  reset() {
    this.current.set({});
  }
}
