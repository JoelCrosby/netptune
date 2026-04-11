import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-menu-checkbox-item]',
  template: `
    <span
      class="flex h-4 w-4 flex-shrink-0 items-center justify-center rounded-sm border border-neutral-300 dark:border-neutral-600"
      [class.bg-primary]="checked()"
      [class.border-primary]="checked()">
      @if (checked()) {
        <svg lucideCheck class="h-3 w-3 text-white"></svg>
      }
    </span>
    <ng-content />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCheck],
  host: {
    class:
      'flex w-full items-center gap-3 px-3 py-2 text-sm text-left cursor-pointer select-none rounded-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800 focus-visible:outline-none focus-visible:bg-neutral-100 dark:focus-visible:bg-neutral-800 disabled:pointer-events-none disabled:opacity-50',
    role: 'menuitemcheckbox',
    '[attr.aria-checked]': 'checked()',
    '(click)': 'toggle()',
  },
})
export class MenuCheckboxItemComponent {
  readonly checked = model(false);

  toggle() {
    this.checked.set(!this.checked());
  }
}
