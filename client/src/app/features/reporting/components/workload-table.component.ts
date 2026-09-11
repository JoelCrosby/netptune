import { Component, computed, input, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { WorkloadRow } from '@core/models/reporting';
import { formatReportValue } from '@core/util/chart-theme';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { queryParams, resetPageOnFilterChange } from './report-table.util';

@Component({
  selector: 'app-workload-table',
  imports: [DatatableComponent],
  host: { class: 'block' },
  template: `
    <app-datatable
      i18n-errorMessage="Shown when the workload breakdown fails to load"
      errorMessage="Workload data could not be loaded."
      i18n-itemLabel="Plural noun for workload rows, used in the row summary"
      itemLabel="assignees"
      [skeletonRows]="5"
      [defaultPageSize]="25"
      [data]="data"
      [(sort)]="sort" />
  `,
})
export class WorkloadTableComponent {
  readonly query = input.required<string>();

  protected readonly sort = signal<DatatableSort | null>(null);

  private readonly datatable = viewChild(DatatableComponent<WorkloadRow>);
  private readonly params = computed<Params>(queryParams(this.query));

  protected readonly data: DatatableDataSource<WorkloadRow> = {
    key: 'workload-rows',
    columns: [
      {
        id: 'displayName',
        header: $localize`:Column heading for the assigned person:Assignee`,
        visibleOnMobile: true,
        accessor: 'displayName',
        sortable: true,
        cellClass: 'font-medium truncate',
      },
      {
        id: 'taskCount',
        header: $localize`:Column heading for the task count:Tasks`,
        visibleOnMobile: true,
        accessor: 'taskCount',
        sortable: true,
        align: 'end',
        widthClass: 'w-28',
        cellClass: 'tabular-nums',
      },
      {
        id: 'value',
        header: $localize`:Column heading for the chosen estimation unit:Selected unit`,
        visibleOnMobile: true,
        accessor: (row) => formatReportValue(row.value),
        sortable: true,
        align: 'end',
        widthClass: 'w-40',
        cellClass: 'tabular-nums',
      },
    ],
    resource: {
      url: 'api/reports/workload/rows',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, row: WorkloadRow) => row.userId ?? 'unassigned',
  };

  constructor() {
    resetPageOnFilterChange(this.params, this.datatable);
  }
}
