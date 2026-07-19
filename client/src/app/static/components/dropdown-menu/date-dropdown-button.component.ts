import { Component, computed, input, output } from '@angular/core';
import {
  cn,
  flatButtonVariants,
  type FlatButtonColor,
} from '../button/button.variants';
import { DatePickerComponent } from '../date-picker/date-picker.component';

@Component({
  selector: 'app-date-dropdown-button',
  imports: [DatePickerComponent],
  template: `
    <app-date-picker
      appearance="bare"
      [label]="label()"
      [value]="value()"
      [disabled]="disabled()"
      [ariaLabel]="ariaLabel() || label()"
      [buttonClass]="className()"
      (valueChange)="valueChanged.emit($event)" />
  `,
})
export class DateDropdownButtonComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly ariaLabel = input<string>();
  readonly color = input<FlatButtonColor>('neutral');
  readonly disabled = input(false);
  readonly buttonClass = input('');
  readonly valueChanged = output<string>();

  protected readonly className = computed(() =>
    cn(
      flatButtonVariants({ color: this.color() }),
      'relative cursor-pointer focus-within:ring-2 focus-within:ring-foreground/30',
      this.buttonClass()
    )
  );
}
