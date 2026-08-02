import { Component, input, model, output } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

/** Compact primary-filled box for selecting rows in a list, without a projected label. */
@Component({
  selector: 'app-selection-checkbox',
  imports: [LucideCheck],
  template: `
    <label
      class="inline-flex cursor-pointer items-center"
      [class.cursor-not-allowed]="disabled()">
      <span
        class="flex h-4 w-4 items-center justify-center rounded-[3px] border-2 transition-colors duration-150"
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

      <input
        type="checkbox"
        class="sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        [attr.aria-label]="label()"
        (change)="onChanged($event)" />
    </label>
  `,
})
export class SelectionCheckboxComponent {
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly label = input<string | null>(null);
  readonly changed = output<boolean>();

  onChanged(event: Event) {
    const input = event.target as HTMLInputElement;

    this.checked.set(input.checked);
    this.changed.emit(input.checked);
  }
}
