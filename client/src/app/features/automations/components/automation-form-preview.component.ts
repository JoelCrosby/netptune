import { Component, input } from '@angular/core';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';
import { AutomationRuleSummaryComponent } from './automation-rule-summary.component';
import { CardContentComponent } from '@app/static/components/card/card-content.component';
import { Status } from '@core/models/status';
import {
  AutomationCopySegment,
  describeAutomationRuleSegments,
} from '../models/automation-copy';
import { AutomationDescriptionComponent } from './automation-description.component';

@Component({
  selector: 'app-automation-form-preview',
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardTitleComponent,
    AutomationRuleSummaryComponent,
    CardContentComponent,
    AutomationDescriptionComponent,
  ],
  template: `
    <aside class="flex flex-col gap-5">
      <app-automation-rule-summary
        [trigger]="trigger()"
        [actions]="actions()"
        [statuses]="statuses()" />

      <app-card>
        <app-card-header>
          <app-card-title>Save Preview</app-card-title>
        </app-card-header>
        <app-card-content>
          <p class="text-foreground text-sm whitespace-pre-wrap">
            <app-automation-description
              [segments]="savePreview()"
              [statuses]="statuses()" />
          </p>
        </app-card-content>
      </app-card>
    </aside>
  `,
})
export class AutomationFormPreviewComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly statuses = input<Status[]>([]);

  savePreview(): AutomationCopySegment[] {
    return describeAutomationRuleSegments(
      this.trigger(),
      this.actions(),
      this.statuses()
    );
  }
}
