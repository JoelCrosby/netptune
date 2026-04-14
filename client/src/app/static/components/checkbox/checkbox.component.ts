import {
  ChangeDetectionStrategy,
  Component,
  input,
  model,
  output,
} from '@angular/core';
import { LucideCheck } from '@lucide/angular';

@Component({
  selector: 'app-checkbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCheck],
  template: `
    <label
      class="flex cursor-pointer items-center justify-center gap-4"
      [class.cursor-not-allowed]="disabled()">
      <div
        class="flex min-h-5 min-w-5 items-center justify-center rounded-[3px] border-2 transition-colors duration-150"
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

      <span class="select-none" [class.opacity-50]="disabled()">
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
  readonly changed = output<boolean>();

  onChanged(event: Event) {
    const input = event.target as HTMLInputElement;
    this.checked.set(input.checked);
    this.changed.emit(input.checked);
  }
}
