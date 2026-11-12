import { Component, computed, input } from '@angular/core';
import { LucideCheck } from '@lucide/angular';
import { AbstractCheckableControl } from '../abstract-checkable-control';

export type CheckboxDensity = 'default' | 'compact';

const labelClasses: Record<CheckboxDensity, string> = {
  default: 'gap-4',
  compact: 'gap-2.5 text-[13px] text-foreground/70 leading-normal',
};

const boxClasses: Record<CheckboxDensity, string> = {
  default: 'min-h-5 min-w-5',
  compact: 'min-h-4.5 min-w-4.5',
};

@Component({
  selector: 'app-checkbox',
  imports: [LucideCheck],
  template: `
    <label
      class="relative flex cursor-pointer items-center before:absolute before:-inset-1"
      [class]="labelClass()"
      [class.cursor-not-allowed]="disabled()">
      <input
        type="checkbox"
        class="peer sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        [attr.aria-label]="ariaLabel()"
        [attr.aria-describedby]="ariaDescribedBy()"
        (change)="onChanged($event)" />

      <div
        class="peer-focus-visible:ring-primary flex items-center justify-center rounded-[3px] border-2 transition-colors duration-150 peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2"
        [class]="boxClass()"
        [class.border-primary]="checked()"
        [class.bg-primary]="checked()"
        [class.border-foreground]="!checked()"
        [class.border-opacity-40]="!checked()"
        [class.opacity-50]="disabled()">
        @if (checked()) {
          <svg
            lucideCheck
            strokeWidth="4"
            class="h-4 w-4 text-white dark:text-black"></svg>
        }
      </div>

      <span class="select-none empty:hidden" [class.opacity-50]="disabled()">
        <ng-content />
      </span>
    </label>
  `,
})
export class CheckboxComponent extends AbstractCheckableControl {
  readonly density = input<CheckboxDensity>('default');
  readonly ariaLabel = input<string | null>(null);

  protected readonly labelClass = computed(() => labelClasses[this.density()]);
  protected readonly boxClass = computed(() => boxClasses[this.density()]);
}
