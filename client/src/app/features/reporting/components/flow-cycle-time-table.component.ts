import { Component, computed, input, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { CycleTimeBucket } from '@core/models/reporting';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { hoursLabel } from './report-format';
import { queryParams, resetPageOnFilterChange } from './report-table.util';

@Component({
  selector: 'app-flow-cycle-time-table',
  imports: [DatatableComponent],
  host: { class: 'block' },
  template: `
    <app-datatable
      containerClass="border-0 border-t rounded-none shadow-none"
      tableClass="md:min-w-140"
      i18n-errorMessage="Shown when the cycle-time breakdown fails to load"
      errorMessage="Cycle-time data could not be loaded."
      i18n-itemLabel="Plural noun for cycle-time weeks, used in the row summary"
      itemLabel="weeks"
      [rounded]="false"
      [skeletonRows]="5"
      [defaultPageSize]="25"
      [data]="data"
      [(sort)]="sort" />
  `,
})
export class FlowCycleTimeTableComponent {
  readonly query = input.required<string>();

  protected readonly sort = signal<DatatableSort | null>(null);

  private readonly datatable = viewChild(DatatableComponent<CycleTimeBucket>);
  private readonly params = computed<Params>(queryParams(this.query));

  protected readonly data: DatatableDataSource<CycleTimeBucket> = {
    key: 'flow-cycle-time',
    columns: [
      {
        id: 'weekStarting',
        header: $localize`:Column heading for the week start date:Week starting`,
        visibleOnMobile: true,
        accessor: 'weekStarting',
        sortable: true,
        cellClass: 'tabular-nums',
      },
      {
        id: 'median',
        header: $localize`:Column heading for the median cycle time:Median`,
        visibleOnMobile: true,
        accessor: (bucket) => hoursLabel(bucket.medianCycleTimeHours),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
      {
        id: 'p85',
        header: $localize`:Column heading for the 85th percentile cycle time:85th percentile`,
        visibleOnMobile: true,
        accessor: (bucket) => hoursLabel(bucket.p85CycleTimeHours),
        sortable: true,
        align: 'end',
        widthClass: 'w-40',
        cellClass: 'tabular-nums',
      },
      {
        id: 'samples',
        header: $localize`:Column heading for the number of samples:Samples`,
        visibleOnMobile: true,
        accessor: 'sampleSize',
        sortable: true,
        align: 'end',
        widthClass: 'w-28',
        cellClass: 'tabular-nums',
      },
    ],
    resource: {
      url: 'api/reports/flow/cycle-time',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, bucket: CycleTimeBucket) => bucket.weekStarting,
  };

  constructor() {
    resetPageOnFilterChange(this.params, this.datatable);
  }
}
