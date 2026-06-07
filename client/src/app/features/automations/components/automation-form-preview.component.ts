import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CardComponent } from '@static/components/card/card.component';
import { AutomationRuleSummaryComponent } from './automation-rule-summary.component';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-form-preview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, AutomationRuleSummaryComponent],
  template: `
    <aside class="flex flex-col gap-4">
      <app-card class="min-h-0! p-5!">
        <app-automation-rule-summary
          [trigger]="trigger()"
          [actions]="actions()" />
      </app-card>

      <app-card class="min-h-0! p-5!">
        <h2 class="text-sm font-semibold">Save Preview</h2>
        <p class="text-foreground/80 mt-2 text-sm">
          {{ savePreview() }}
        </p>
      </app-card>
    </aside>
  `,
})
export class AutomationFormPreviewComponent {
  readonly trigger = input.required<AutomationTrigger>();
  readonly actions = input.required<AutomationAction[]>();
  readonly savePreview = input.required<string>();
}
