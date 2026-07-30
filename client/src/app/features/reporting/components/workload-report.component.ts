import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';
import { WorkloadReport } from '@core/models/reporting';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
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
    ErrorStateComponent,
    EmptyStateComponent,
    PageLoadingComponent,
    SectionHeaderComponent,
    StatComponent,
    TableComponent,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <app-section-header
        i18n-heading="Section heading for the workload report"
        heading="Current workload"
        i18n-description="Explains what the workload report shows"
        description="Open work by assignee. Multi-assigned tasks appear for every assignee." />

      @if (resource.isLoading()) {
        <div class="h-40">
          <app-page-loading
            i18n-label="Shown while the workload report loads"
            label="Loading workload" />
        </div>
      } @else if (resource.error()) {
        <app-error-state
          compact
          i18n-title="Shown when the workload report fails to load"
          title="Workload could not be loaded"
          i18n-description="Advice when the workload report fails to load"
          description="Retry the request to load workload reporting."
          (retry)="resource.reload()" />
      } @else if (resource.value(); as report) {
        <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <app-stat
            i18n-label="Stat label for distinct open tasks"
            label="Unique open tasks"
            [value]="report.uniqueTaskCount" />
          <app-stat
            i18n-label="Stat label for open tasks with nobody assigned"
            label="Unassigned"
            [value]="report.unassignedTaskCount" />
          <app-stat
            i18n-label="Stat label for tasks with several assignees"
            label="Multi-assigned"
            [value]="report.multiAssignedTaskCount" />
          <app-stat
            i18n-label="Stat label for tasks without an estimate"
            label="Missing estimate"
            [value]="report.missingEstimateCount" />
        </div>

        @if (report.rows.length) {
          <app-table>
            <thead appTableHead>
              <tr appTableHeaderRow>
                <th class="px-4 py-3">
                  <span i18n="Column heading for the assigned person">
                    Assignee
                  </span>
                </th>
                <th class="px-4 py-3">
                  <span i18n="Column heading for the task count">Tasks</span>
                </th>
                <th class="px-4 py-3">
                  <span i18n="Column heading for the chosen estimation unit">
                    Selected unit
                  </span>
                </th>
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
            i18n-title="Empty state for the workload report"
            title="No open assigned work"
            i18n-description="Explains the empty workload state"
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
