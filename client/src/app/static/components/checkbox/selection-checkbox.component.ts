import { Component, input } from '@angular/core';
import { LucideCheck } from '@lucide/angular';
import { AbstractCheckableControl } from '../abstract-checkable-control';

/** Compact primary-filled box for selecting rows in a list, without a projected label. */
@Component({
  selector: 'app-selection-checkbox',
  imports: [LucideCheck],
  template: `
    <label
      class="relative inline-flex cursor-pointer items-center before:absolute before:-inset-1"
      [class.cursor-not-allowed]="disabled()">
      <input
        type="checkbox"
        class="peer sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        [attr.aria-label]="label()"
        [attr.aria-describedby]="ariaDescribedBy()"
        (change)="onChanged($event)" />

      <span
        class="peer-focus-visible:ring-primary flex h-4 w-4 items-center justify-center rounded-[3px] border-2 transition-colors duration-150 peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2"
        [class.border-primary]="checked()"
        [class.bg-primary]="checked()"
        [class.border-foreground]="!checked()"
        [class.border-opacity-40]="!checked()"
        [class.opacity-50]="disabled()">
        @if (checked()) {
          <svg
            lucideCheck
            strokeWidth="4"
            class="text-primary-foreground h-3 w-3"></svg>
        }
      </span>
    </label>
  `,
})
export class SelectionCheckboxComponent extends AbstractCheckableControl {
  readonly label = input<string | null>(null);
}
