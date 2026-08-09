import { Component, input } from '@angular/core';
import { StatComponent } from '@static/components/stat/stat.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AutomationRule, AutomationRun } from '../models/automation.models';

@Component({
  selector: 'app-automation-detail-stats',
  imports: [StatComponent, PrettyDatePipe],
  template: `
    <div class="grid gap-4 md:grid-cols-3">
      <app-stat
        i18n-label="Stat label for how many actions a rule has"
        label="Actions"
        [value]="rule().actions.length" />
      <app-stat
        i18n-label="Stat label for how many times a rule has run"
        label="Total Runs"
        [value]="totalRuns()" />
      <app-stat
        i18n-label="Stat label for when a rule last ran"
        label="Last Run"
        [value]="
          lastRun() ? (lastRun()!.createdAt | prettyDate) : 'Not run yet'
        " />
    </div>
  `,
})
export class AutomationDetailStatsComponent {
  readonly rule = input.required<AutomationRule>();
  readonly totalRuns = input.required<number>();
  readonly lastRun = input.required<AutomationRun | null>();
}
