import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import {
  describeAutomationActions,
  describeAutomationRule,
  describeAutomationTrigger,
} from '../models/automation-copy';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-rule-summary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-3">
      <div>
        <h2 class="text-sm font-semibold">Rule Preview</h2>
        <p class="text-foreground/80 mt-1 text-sm">
          {{ ruleSummary() }}
        </p>
      </div>

      <div class="grid gap-3 md:grid-cols-2">
        <div class="border-border rounded border p-3">
          <h3
            class="text-muted mb-1 text-xs font-semibold tracking-wide uppercase">
            When
          </h3>
          <p class="text-sm">{{ triggerSummary() }}</p>
        </div>

        <div class="border-border rounded border p-3">
          <h3
            class="text-muted mb-1 text-xs font-semibold tracking-wide uppercase">
            Then
          </h3>
          <p class="text-sm">{{ actionsSummary() }}</p>
        </div>
      </div>
    </div>
  `,
})
export class AutomationRuleSummaryComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();

  ruleSummary(): string {
    return describeAutomationRule(this.trigger(), this.actions());
  }

  triggerSummary(): string {
    return describeAutomationTrigger(this.trigger());
  }

  actionsSummary(): string {
    return describeAutomationActions(this.actions());
  }
}
