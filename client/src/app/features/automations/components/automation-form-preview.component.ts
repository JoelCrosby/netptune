import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CardComponent } from '@static/components/card/card.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { AutomationRuleSummaryComponent } from './automation-rule-summary.component';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-form-preview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    AutomationRuleSummaryComponent,
  ],
  template: `
    <aside class="flex flex-col gap-4">
      <app-card class="min-h-0! p-5!">
        <app-automation-rule-summary
          [trigger]="trigger()"
          [actions]="actions()" />
      </app-card>

      <app-card class="min-h-0! p-5!">
        <app-card-header>
          <app-card-title>Save Preview</app-card-title>
          <app-card-subtitle>
            {{ savePreview() }}
          </app-card-subtitle>
        </app-card-header>
      </app-card>
    </aside>
  `,
})
export class AutomationFormPreviewComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly savePreview = input.required<string>();
}
