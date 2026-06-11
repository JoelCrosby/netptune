import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import {
  StatComponent,
  StatValue,
} from '@static/components/stat/stat.component';

export interface AutomationStat {
  label: string;
  value: StatValue;
}

@Component({
  selector: 'app-automation-stat-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StatComponent],
  template: `
    <div class="grid gap-3 md:grid-cols-3">
      @for (stat of stats(); track stat.label) {
        <app-stat [label]="stat.label" [value]="stat.value" />
      }
    </div>
  `,
})
export class AutomationStatGridComponent {
  readonly stats = input.required<AutomationStat[]>();
}
