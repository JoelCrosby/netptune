import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CardComponent } from '@static/components/card/card.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AutomationRule, AutomationRun } from '../models/automation.models';

@Component({
  selector: 'app-automation-detail-stats',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, PrettyDatePipe],
  template: `
    <div class="grid gap-4 md:grid-cols-3">
      <app-card class="min-h-0! p-4!">
        <span class="text-muted text-xs font-medium uppercase">Actions</span>
        <strong class="mt-1 text-2xl">{{ rule().actions.length }}</strong>
      </app-card>
      <app-card class="min-h-0! p-4!">
        <span class="text-muted text-xs font-medium uppercase"
          >Recent Runs</span
        >
        <strong class="mt-1 text-2xl">{{ runs().length }}</strong>
      </app-card>
      <app-card class="min-h-0! p-4!">
        <span class="text-muted text-xs font-medium uppercase">Last Run</span>
        <strong class="mt-2 text-sm">
          @if (runs()[0]; as lastRun) {
            {{ lastRun.createdAt | prettyDate }}
          } @else {
            Not run yet
          }
        </strong>
      </app-card>
    </div>
  `,
})
export class AutomationDetailStatsComponent {
  readonly rule = input.required<AutomationRule>();
  readonly runs = input.required<AutomationRun[]>();
}
