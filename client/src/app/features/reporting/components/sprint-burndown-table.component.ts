import { Component, computed, input, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { BurndownPoint, ReportingUnit } from '@core/models/reporting';
import { formatReportValue } from '@core/util/chart-theme';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { resetPageOnFilterChange } from './report-table.util';

@Component({
  selector: 'app-sprint-burndown-table',
  imports: [DatatableComponent],
  host: { class: 'block' },
  template: `
    <app-datatable
      containerClass="border-0 border-t rounded-none shadow-none"
      tableClass="md:min-w-140"
      i18n-errorMessage="Shown when the burndown breakdown fails to load"
      errorMessage="Burndown data could not be loaded."
      i18n-itemLabel="Plural noun for burndown days, used in the row summary"
      itemLabel="days"
      [rounded]="false"
      [skeletonRows]="5"
      [defaultPageSize]="25"
      [data]="data"
      [(sort)]="sort" />
  `,
})
export class SprintBurndownTableComponent {
  readonly sprintId = input.required<number>();
  readonly unit = input.required<ReportingUnit>();
  readonly timeZone = input.required<string>();

  protected readonly sort = signal<DatatableSort | null>(null);

  private readonly datatable = viewChild(DatatableComponent<BurndownPoint>);

  private readonly params = computed<Params>(() => {
    return { unit: this.unit(), timeZone: this.timeZone() };
  });

  // The sprint sits in the path, so a different sprint is a different resource.
  private readonly url = computed(() => {
    return `api/reports/sprints/${this.sprintId()}/burndown/points`;
  });

  protected readonly data: DatatableDataSource<BurndownPoint> = {
    key: 'sprint-burndown-points',
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
        id: 'remaining',
        header: $localize`:Column heading for remaining scope:Remaining`,
        visibleOnMobile: true,
        accessor: (point) => formatReportValue(point.remaining),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
      {
        id: 'totalScope',
        header: $localize`:Column heading for total scope:Total scope`,
        visibleOnMobile: true,
        accessor: (point) => formatReportValue(point.totalScope),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
      {
        id: 'ideal',
        header: $localize`:Column heading for the ideal burndown value:Ideal`,
        visibleOnMobile: true,
        accessor: (point) => formatReportValue(point.ideal),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
    ],
    resource: {
      url: this.url,
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, point: BurndownPoint) => point.date,
  };

  constructor() {
    resetPageOnFilterChange(this.params, this.datatable);
  }
}
