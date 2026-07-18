import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { ReportingCoverage } from '@core/models/reporting';
import { BadgeComponent } from '@static/components/badge/badge.component';

@Component({
  selector: 'app-report-coverage-notice',
  template: `
    @if (coverage(); as value) {
      @if (value.isPartial) {
        <div
          class="border-border bg-card flex items-center gap-3 rounded border p-3 text-sm">
          <app-badge color="info">Partial history</app-badge>
          <p class="text-muted">
            Reporting history begins
            {{
              value.coverageStart
                ? (value.coverageStart | date: 'medium')
                : 'with the next recorded change'
            }}. Earlier activity is not estimated.
          </p>
        </div>
      }
    }
  `,
  imports: [BadgeComponent, DatePipe],
})
export class ReportCoverageNoticeComponent {
  readonly coverage = input<ReportingCoverage>();
}
