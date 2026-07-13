import { Component, input } from '@angular/core';
import { BadgeComponent } from '@static/components/badge/badge.component';

@Component({
  selector: 'app-automation-enabled-badge',
  imports: [BadgeComponent],
  template: `
    <app-badge shape="rounded" [color]="enabled() ? 'success' : 'neutral'">
      {{ enabled() ? 'Enabled' : 'Disabled' }}
    </app-badge>
  `,
})
export class AutomationEnabledBadgeComponent {
  readonly enabled = input.required<boolean>();
}
