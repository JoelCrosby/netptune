import { Component, input } from '@angular/core';
import { AbstractCheckableControl } from '../abstract-checkable-control';

@Component({
  selector: 'app-switch',
  template: `
    <label
      class="inline-flex cursor-pointer items-center"
      [class.cursor-not-allowed]="disabled()">
      <input
        type="checkbox"
        role="switch"
        class="peer sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        [attr.aria-label]="ariaLabel()"
        (change)="onChanged($event)" />

      <span
        class="peer-focus-visible:ring-primary relative flex h-5.25 w-9.5 shrink-0 items-center rounded-full p-0.5 transition-colors duration-150 peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2 peer-disabled:opacity-50"
        [class]="checked() ? 'bg-primary' : 'bg-foreground/20'">
        <span
          class="block h-4.25 w-4.25 rounded-full transition-transform duration-150"
          [class]="
            checked()
              ? 'translate-x-4.25 bg-white dark:bg-black/80'
              : 'translate-x-0 bg-white dark:bg-white/85'
          "></span>
      </span>
    </label>
  `,
})
export class SwitchComponent extends AbstractCheckableControl {
  readonly ariaLabel = input<string | null>(null);
}
