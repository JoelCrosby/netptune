import { Component, input } from '@angular/core';
import { BadgeComponent } from '@static/components/badge/badge.component';

@Component({
  selector: 'app-automation-enabled-badge',
  imports: [BadgeComponent],
  template: `
    <app-badge shape="rounded" [color]="enabled() ? 'success' : 'neutral'">
      {{ stateLabel() }}
    </app-badge>
  `,
})
export class AutomationEnabledBadgeComponent {
  /** Ternaries in a template expression cannot be marked, so build the copy here. */
  protected stateLabel(): string {
    return this.enabled()
      ? $localize`:Badge marking an automation that is switched on:Enabled`
      : $localize`:Badge marking an automation that is switched off:Disabled`;
  }

  readonly enabled = input.required<boolean>();
}
