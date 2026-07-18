import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import { WorkloadReport } from '@core/models/reporting';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { StatComponent } from '@static/components/stat/stat.component';
import {
  TableComponent,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-workload-report',
  imports: [
    EmptyStateComponent,
    PageLoadingComponent,
    SectionHeaderComponent,
    StatComponent,
    StrokedButtonComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <app-section-header
        heading="Current workload"
        description="Open work by assignee. Multi-assigned tasks appear for every assignee." />

      @if (resource.isLoading()) {
        <div class="h-40">
          <app-page-loading label="Loading workload" />
        </div>
      } @else if (resource.error()) {
        <app-empty-state
          compact
          title="Workload could not be loaded"
          description="Retry the request to load workload reporting.">
          <button
            emptyStateAction
            app-stroked-button
            type="button"
            (click)="resource.reload()">
            Retry
          </button>
        </app-empty-state>
      } @else if (resource.value(); as report) {
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <app-stat
            label="Unique open tasks"
            [value]="report.uniqueTaskCount" />
          <app-stat label="Unassigned" [value]="report.unassignedTaskCount" />
          <app-stat
            label="Multi-assigned"
            [value]="report.multiAssignedTaskCount" />
          <app-stat
            label="Missing estimate"
            [value]="report.missingEstimateCount" />
        </div>

        @if (report.rows.length) {
          <app-table>
            <thead appTableHead>
              <tr appTableHeaderRow>
                <th class="px-4 py-3">Assignee</th>
                <th class="px-4 py-3">Tasks</th>
                <th class="px-4 py-3">Selected unit</th>
              </tr>
            </thead>
            <tbody>
              @for (row of report.rows; track row.userId ?? 'unassigned') {
                <tr appTableRow>
                  <td class="px-4 py-2.5 font-medium">
                    {{ row.displayName }}
                  </td>
                  <td class="px-4 py-2.5">{{ row.taskCount }}</td>
                  <td class="px-4 py-2.5">{{ row.value }}</td>
                </tr>
              }
            </tbody>
          </app-table>
        } @else {
          <app-empty-state
            compact
            title="No open assigned work"
            description="There is no open assigned work for this selection." />
        }
      }
    </section>
  `,
})
export class WorkloadReportComponent {
  readonly query = input.required<string>();
  readonly resource = httpResource<WorkloadReport>(
    () => `api/reports/workload?${this.query()}`
  );
}
