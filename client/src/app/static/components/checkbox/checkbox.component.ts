import { Component, computed, input, model, output } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

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
      class="flex cursor-pointer items-center"
      [class]="labelClass()"
      [class.cursor-not-allowed]="disabled()">
      <div
        class="flex items-center justify-center rounded-[3px] border-2 transition-colors duration-150"
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

      <input
        type="checkbox"
        class="sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        (change)="onChanged($event)" />
    </label>
  `,
})
export class CheckboxComponent {
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly density = input<CheckboxDensity>('default');
  readonly changed = output<boolean>();

  protected readonly labelClass = computed(() => labelClasses[this.density()]);
  protected readonly boxClass = computed(() => boxClasses[this.density()]);

  onChanged(event: Event) {
    const input = event.target as HTMLInputElement;
    this.checked.set(input.checked);
    this.changed.emit(input.checked);
  }
}
