import { Component, computed, input, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { ReportingUnit, VelocityPoint } from '@core/models/reporting';
import { formatReportValue } from '@core/util/chart-theme';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { resetPageOnFilterChange } from './report-table.util';

@Component({
  selector: 'app-sprint-velocity-table',
  imports: [DatatableComponent],
  host: { class: 'block' },
  template: `
    <app-datatable
      containerClass="border-0 border-t rounded-none shadow-none"
      tableClass="md:min-w-180"
      i18n-errorMessage="Shown when the velocity breakdown fails to load"
      errorMessage="Velocity data could not be loaded."
      i18n-itemLabel="Plural noun for velocity sprints, used in the row summary"
      itemLabel="sprints"
      [rounded]="false"
      [skeletonRows]="5"
      [defaultPageSize]="25"
      [data]="data"
      [(sort)]="sort" />
  `,
})
export class SprintVelocityTableComponent {
  readonly projectId = input.required<number>();
  readonly unit = input.required<ReportingUnit>();
  readonly take = input.required<number>();

  protected readonly sort = signal<DatatableSort | null>(null);

  private readonly datatable = viewChild(DatatableComponent<VelocityPoint>);

  private readonly params = computed<Params>(() => {
    return {
      projectId: this.projectId(),
      unit: this.unit(),
      take: this.take(),
    };
  });

  protected readonly data: DatatableDataSource<VelocityPoint> = {
    key: 'velocity-sprints',
    columns: [
      {
        id: 'sprintName',
        header: $localize`:Column heading for the sprint name:Sprint`,
        visibleOnMobile: true,
        accessor: 'sprintName',
        sortable: true,
        cellClass: 'font-medium truncate',
      },
      {
        id: 'committed',
        header: $localize`:Column heading for committed scope:Committed`,
        visibleOnMobile: true,
        accessor: (point) => formatReportValue(point.committed),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
      {
        id: 'completed',
        header: $localize`:Column heading for completed scope:Completed`,
        visibleOnMobile: true,
        accessor: (point) => formatReportValue(point.completed),
        sortable: true,
        align: 'end',
        widthClass: 'w-32',
        cellClass: 'tabular-nums',
      },
      {
        id: 'missingEstimateCount',
        header: $localize`:Column heading for tasks without an estimate:Missing estimate`,
        visibleOnMobile: false,
        accessor: 'missingEstimateCount',
        sortable: true,
        align: 'end',
        widthClass: 'w-40',
        cellClass: 'tabular-nums',
      },
      {
        id: 'differentUnitEstimateCount',
        header: $localize`:Column heading for tasks estimated in another unit:Different unit`,
        visibleOnMobile: false,
        accessor: 'differentUnitEstimateCount',
        sortable: true,
        align: 'end',
        widthClass: 'w-40',
        cellClass: 'tabular-nums',
      },
    ],
    resource: {
      url: 'api/reports/velocity/sprints',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, point: VelocityPoint) => point.sprintId,
  };

  constructor() {
    resetPageOnFilterChange(this.params, this.datatable);
  }
}
