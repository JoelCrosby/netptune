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

@Component({
  selector: 'app-automation-form-preview',
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardTitleComponent,
    AutomationRuleSummaryComponent,
    CardContentComponent,
  ],
  template: `
    <aside class="flex flex-col gap-5">
      <app-automation-rule-summary
        [trigger]="trigger()"
        [actions]="actions()" />

      <app-card>
        <app-card-header>
          <app-card-title>Save Preview</app-card-title>
        </app-card-header>
        <app-card-content>
          <p
            class="text-foreground text-sm whitespace-pre-wrap"
            [innerHtml]="savePreview()"></p>
        </app-card-content>
      </app-card>
    </aside>
  `,
})
export class AutomationFormPreviewComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly savePreview = input.required<string>();
}
