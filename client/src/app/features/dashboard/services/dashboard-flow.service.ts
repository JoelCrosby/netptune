import { httpResource } from '@angular/common/http';
import { Injectable, computed } from '@angular/core';
import { FlowReport } from '@core/models/reporting';
import { hostTimeZone, isoDateValue } from '@core/util/dates';

const trailingDays = 30;

/**
 * The throughput and cycle-time cards are two views of one flow report, so the
 * request lives here and is provided once by the dashboard view rather than
 * fetched twice.
 */
@Injectable()
export class DashboardFlowService {
  readonly resource = httpResource<FlowReport>(() => {
    const to = new Date();
    const from = new Date(to);
    from.setDate(from.getDate() - trailingDays);

    return {
      url: 'api/reports/flow',
      params: {
        from: isoDateValue(from),
        to: isoDateValue(to),
        unit: 'Tasks',
        grouping: 'Day',
        timeZone: hostTimeZone(),
      },
    };
  });

  readonly report = computed(() => this.resource.value() ?? null);

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  readonly failed = computed(() => Boolean(this.resource.error()));

  readonly trailingDays = trailingDays;
}
