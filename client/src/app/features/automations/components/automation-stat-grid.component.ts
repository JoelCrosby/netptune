import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CardComponent } from '@static/components/card/card.component';

export interface AutomationStat {
  label: string;
  value: number | string;
}

@Component({
  selector: 'app-automation-stat-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  template: `
    <div class="grid gap-3 md:grid-cols-3">
      @for (stat of stats(); track stat.label) {
        <app-card class="min-h-0! p-4!">
          <span class="text-muted text-xs font-medium uppercase">
            {{ stat.label }}
          </span>
          <strong class="mt-1 text-2xl">{{ stat.value }}</strong>
        </app-card>
      }
    </div>
  `,
})
export class AutomationStatGridComponent {
  readonly stats = input.required<AutomationStat[]>();
}
