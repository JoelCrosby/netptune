import {
  Component,
  computed,
  effect,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Params } from '@angular/router';
import { FlowBucket } from '@core/models/reporting';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';

@Component({
  selector: 'app-flow-throughput-table',
  imports: [DatatableComponent],
  host: { class: 'block' },
  template: `
    <app-datatable
      containerClass="border-0 border-t rounded-none shadow-none"
      i18n-errorMessage="Shown when the throughput breakdown fails to load"
      errorMessage="Throughput data could not be loaded."
      i18n-itemLabel="
        Plural noun for throughput periods, used in the row summary
      "
      itemLabel="periods"
      [rounded]="false"
      [skeletonRows]="5"
      [defaultPageSize]="25"
      [data]="data"
      [(sort)]="sort" />
  `,
})
export class FlowThroughputTableComponent {
  readonly query = input.required<string>();

  protected readonly sort = signal<DatatableSort | null>(null);

  private readonly datatable = viewChild(DatatableComponent<FlowBucket>);

  // The report filters arrive as a query string, which the table merges with its
  // own paging and sort parameters.
  private readonly params = computed<Params>(() => {
    return Object.fromEntries(new URLSearchParams(this.query()));
  });

  protected readonly data: DatatableDataSource<FlowBucket> = {
    key: 'flow-throughput',
    columns: [
      {
        id: 'date',
        header: $localize`:Column heading for the date:Date`,
        visibleOnMobile: true,
        accessor: 'date',
        sortable: true,
        cellClass: 'tabular-nums',
      },
      {
        id: 'completed',
        header: $localize`:Column heading for the completed count:Completed`,
        visibleOnMobile: true,
        accessor: 'completed',
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
    ],
    resource: {
      url: 'api/reports/flow/throughput',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, bucket: FlowBucket) => bucket.date,
  };

  constructor() {
    let previousQuery: string | null = null;

    // A new report range is a different set of periods, so page three of the old
    // one means nothing here. The first run only records the starting filters.
    effect(() => {
      const query = this.query();
      const isFirstRun = previousQuery === null;
      const hasChanged = query !== previousQuery;

      previousQuery = query;

      if (isFirstRun || !hasChanged) return;

      this.datatable()?.goToPage(1);
    });
  }
}
