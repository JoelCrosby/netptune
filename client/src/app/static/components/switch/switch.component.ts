import { Component, input, model, output } from '@angular/core';

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
        class="peer-focus-visible:ring-primary relative flex h-5 w-9 shrink-0 items-center rounded-full transition-colors peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2 peer-disabled:opacity-50"
        [class]="checked() ? 'bg-primary' : 'bg-foreground/25'">
        <span
          class="h-4 w-4 rounded-full bg-white shadow-sm transition-transform"
          [class]="checked() ? 'translate-x-4.5' : 'translate-x-0.5'"></span>
      </span>
    </label>
  `,
})
export class SwitchComponent {
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly ariaLabel = input<string | null>(null);

  readonly changed = output<boolean>();

  protected onChanged(event: Event) {
    const input = event.target as HTMLInputElement;

    this.checked.set(input.checked);
    this.changed.emit(input.checked);
  }
}
