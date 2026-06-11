import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { StatComponent } from '@static/components/stat/stat.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AutomationRule, AutomationRun } from '../models/automation.models';

@Component({
  selector: 'app-automation-detail-stats',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StatComponent, PrettyDatePipe],
  template: `
    <div class="grid gap-4 md:grid-cols-3">
      <app-stat label="Actions" [value]="rule().actions.length" />
      <app-stat label="Recent Runs" [value]="runs().length" />
      <app-stat
        label="Last Run"
        [value]="
          runs()[0] ? (runs()[0].createdAt | prettyDate) : 'Not run yet'
        " />
    </div>
  `,
})
export class AutomationDetailStatsComponent {
  readonly rule = input.required<AutomationRule>();
  readonly runs = input.required<AutomationRun[]>();
}
