import { Component, input, model } from '@angular/core';
import { LucideSearch, LucideX } from '@lucide/angular';
import { FormControlFieldComponent } from '../form-control/form-control-field.component';
import { FormControlInputDirective } from '../form-control/form-control.directives';

@Component({
  selector: 'app-filter-input',
  host: { class: 'block' },
  imports: [
    LucideSearch,
    LucideX,
    FormControlFieldComponent,
    FormControlInputDirective,
  ],
  template: `
    <app-form-control-field density="compact" class="gap-1.5 px-2.5">
      <svg lucideSearch class="text-muted h-4 w-4 shrink-0"></svg>

      <input
        appFormInput
        type="text"
        class="text-sm leading-none!"
        [value]="value()"
        [attr.placeholder]="placeholder()"
        [attr.aria-label]="ariaLabel() ?? placeholder()"
        (input)="setValue($event)" />

      @if (value().length > 0) {
        <button
          type="button"
          class="text-muted hover:text-foreground flex h-4 w-4 shrink-0 items-center justify-center transition-colors"
          i18n-aria-label="Accessible label for the button that clears a filter"
          aria-label="Clear filter"
          (click)="value.set('')">
          <svg lucideX class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      }
    </app-form-control-field>
  `,
})
export class FilterInputComponent {
  readonly value = model('');
  readonly placeholder = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);

  protected setValue(event: Event) {
    this.value.set((event.target as HTMLInputElement).value);
  }
}
