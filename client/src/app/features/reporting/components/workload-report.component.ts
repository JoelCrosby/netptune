import { Component, computed, input } from '@angular/core';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { workloadReportResource } from '@core/resources/reporting.resource';
import { WorkloadTableComponent } from './workload-table.component';
import { PanelComponent } from '@static/components/panel.component';

@Component({
  selector: 'app-workload-report',
  imports: [
    EmptyStateComponent,
    ErrorStateComponent,
    PanelComponent,
    SectionHeaderComponent,
    SkeletonComponent,
    StatStripComponent,
    WorkloadTableComponent,
  ],
  template: `
    <section class="flex flex-col gap-6">
      <app-section-header
        i18n-heading="Section heading for the workload report"
        heading="Current workload"
        i18n-description="Explains what the workload report shows"
        description="Open work by assignee. Multi-assigned tasks appear for every assignee." />

      @if (resource.isLoading()) {
        <app-panel
          surface="card"
          class="p-6"
          role="status"
          i18n-aria-label="Shown while the workload report loads"
          aria-label="Loading workload">
          <app-skeleton class="h-10 w-full" />
          <app-skeleton class="mt-6 h-32 w-full" />
        </app-panel>
      } @else if (resource.error()) {
        <app-error-state
          compact
          i18n-title="Shown when the workload report fails to load"
          title="Workload could not be loaded"
          i18n-description="Advice when the workload report fails to load"
          description="Retry the request to load workload reporting."
          (retry)="resource.reload()" />
      } @else if (resource.value(); as report) {
        <section app-panel surface="card">
          <app-stat-strip [items]="stats()" />
        </section>

        @if (report.rows.length) {
          <app-workload-table [query]="query()" />
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
  readonly resource = workloadReportResource(
    computed(() => Object.fromEntries(new URLSearchParams(this.query())))
  );

  protected readonly stats = computed<StatStripItem[]>(() => {
    const report = this.resource.value();

    if (!report) return [];

    return [
      {
        label: $localize`:Stat label for distinct open tasks:Unique open tasks`,
        value: report.uniqueTaskCount,
      },
      {
        label: $localize`:Stat label for open tasks with nobody assigned:Unassigned`,
        value: report.unassignedTaskCount,
      },
      {
        label: $localize`:Stat label for tasks with several assignees:Multi-assigned`,
        value: report.multiAssignedTaskCount,
      },
      {
        label: $localize`:Stat label for tasks without an estimate:Missing estimate`,
        value: report.missingEstimateCount,
      },
    ];
  });
}
