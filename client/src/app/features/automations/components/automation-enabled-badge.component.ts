import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-automation-enabled-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="'rounded px-2 py-0.5 text-xs font-medium ' + badgeClass()">
      {{ enabled() ? 'Enabled' : 'Disabled' }}
    </span>
  `,
})
export class AutomationEnabledBadgeComponent {
  readonly enabled = input.required<boolean>();

  badgeClass(): string {
    return this.enabled()
      ? 'bg-green-500/10 text-green-600 dark:text-green-400'
      : 'bg-foreground/10 text-foreground/70';
  }
}
