import { Component, input } from '@angular/core';
import { CardComponent } from '@app/static/components/card/card.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { Status } from '@core/models/status';
import {
  AutomationCopySegment,
  describeAutomationActionsSegments,
  describeAutomationRuleSegments,
  describeAutomationTriggerSegments,
} from '../models/automation-copy';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';

@Component({
  selector: 'app-automation-rule-summary',
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    AutomationDescriptionComponent,
  ],
  template: `
    <app-card>
      <app-card-header>
        <app-card-title>Rule Preview</app-card-title>
        <app-card-subtitle>
          <app-automation-description
            [segments]="ruleSummary()"
            [statuses]="statuses()" />
        </app-card-subtitle>
      </app-card-header>

      <div class="flex flex-col gap-3">
        <div class="border-border border-b pb-3">
          <h3
            class="text-muted mb-1 text-xs font-semibold tracking-wide uppercase">
            When
          </h3>
          <p class="text-sm">
            <app-automation-description
              [segments]="triggerSummary()"
              [statuses]="statuses()" />
          </p>
        </div>

        <div>
          <h3
            class="text-muted mb-1 text-xs font-semibold tracking-wide uppercase">
            Then
          </h3>
          <p class="text-sm">
            <app-automation-description
              [segments]="actionsSummary()"
              [statuses]="statuses()" />
          </p>
        </div>
      </div>
    </app-card>
  `,
})
export class AutomationRuleSummaryComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);

  ruleSummary(): AutomationCopySegment[] {
    return describeAutomationRuleSegments(
      this.trigger(),
      this.actions(),
      this.statuses()
    );
  }

  triggerSummary(): AutomationCopySegment[] {
    return describeAutomationTriggerSegments(this.trigger(), this.statuses());
  }

  actionsSummary(): AutomationCopySegment[] {
    return describeAutomationActionsSegments(this.actions(), this.statuses());
  }
}
