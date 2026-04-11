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
      class="flex cursor-pointer items-center justify-center"
      [class.cursor-not-allowed]="disabled()">
      <input
        type="checkbox"
        class="sr-only"
        [checked]="checked()"
        [disabled]="disabled()"
        (change)="onChanged($event)" />
      <div
        class="flex h-4 w-4 items-center justify-center rounded-[3px] border-2 transition-colors duration-150"
        [class.border-primary]="checked()"
        [class.bg-primary]="checked()"
        [class.border-foreground]="!checked()"
        [class.border-opacity-40]="!checked()"
        [class.opacity-50]="disabled()">
        @if (checked()) {
          <svg lucideCheck class="h-2.5 w-2.5 text-white"></svg>
        }
      </div>
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
