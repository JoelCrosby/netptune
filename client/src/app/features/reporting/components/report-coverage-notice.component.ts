import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { ReportingCoverage } from '@core/models/reporting';
import { BadgeComponent } from '@static/components/badge/badge.component';

@Component({
  selector: 'app-report-coverage-notice',
  template: `
    @if (coverage(); as value) {
      @if (value.isPartial) {
        <div
          class="border-border bg-card flex items-center gap-3 rounded border p-3 text-sm">
          <app-badge
            color="info"
            i18n="Badge marking reports with incomplete history">
            Partial history
          </app-badge>
          <p class="text-muted">
            <span
              i18n="
                Explains when reporting data starts. START is either a formatted
                date or a phrase such as 'with the next recorded change'
              ">
              Reporting history begins
              {{
                coverageStartLabel(value)  // i18n(ph="START")
              }}. Earlier activity is not estimated.
            </span>
          </p>
        </div>
      }
    }
  `,
  imports: [BadgeComponent],
  providers: [DatePipe],
})
export class ReportCoverageNoticeComponent {
  private readonly datePipe = inject(DatePipe);

  readonly coverage = input<ReportingCoverage>();

  /** The fallback phrase cannot be marked inside a template expression. */
  protected coverageStartLabel(value: ReportingCoverage): string {
    if (!value.coverageStart) {
      return $localize`:Stands in for a start date when reporting history has not begun yet:with the next recorded change`;
    }

    return this.datePipe.transform(value.coverageStart, 'medium') ?? '';
  }
}
