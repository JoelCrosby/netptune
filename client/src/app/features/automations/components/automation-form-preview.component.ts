import { Component, input } from '@angular/core';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';
import { AutomationRuleSummaryComponent } from './automation-rule-summary.component';
import { Status } from '@core/models/status';

@Component({
  selector: 'app-automation-form-preview',
  imports: [AutomationRuleSummaryComponent],
  template: `
    <aside class="lg:sticky lg:top-5">
      <app-automation-rule-summary
        [trigger]="trigger()"
        [actions]="actions()"
        [statuses]="statuses()" />
    </aside>
  `,
})
export class AutomationFormPreviewComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);
}
