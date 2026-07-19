import { Component, input, output } from '@angular/core';
import { type FlatButtonColor } from '../button/button.variants';
import { DatePickerComponent } from '../date-picker/date-picker.component';

@Component({
  selector: 'app-date-dropdown-button',
  imports: [DatePickerComponent],
  template: `
    <app-date-picker
      appearance="flat"
      [label]="label()"
      [value]="value()"
      [color]="color()"
      [disabled]="disabled()"
      [ariaLabel]="ariaLabel() || label()"
      [buttonClass]="buttonClass()"
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
}
