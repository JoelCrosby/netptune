import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  inject,
  input,
  output,
  viewChild
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-form-textarea',
  templateUrl: './form-textarea.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon],
})
export class FormTextAreaComponent implements ControlValueAccessor {
  ngControl = inject(NgControl, { self: true, optional: true });

  @Input() label!: string;
  @Input() disabled!: boolean;
  @Input() icon!: string;
  @Input() prefix!: string;
  readonly placeholder = input<string | null>(null);
  @Input() hint: string | null = null;
  readonly minLength = input<string | null>(null);
  readonly maxLength = input<string | null>(null);
  readonly rows = input('2');

  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();

  value: string | number = '';

  onChange!: (value: string) => void;
  onTouch!: () => void;

  get control() {
    return this.ngControl?.control;
  }

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.onChange(value);
    this.onTouch();
  }

  writeValue(value: string) {
    this.value = value;
  }

  registerOnChange(fn: (...args: unknown[]) => unknown) {
    this.onChange = fn;
  }

  registerOnTouched(fn: (...args: unknown[]) => unknown) {
    this.onTouch = fn;
  }

  setDisabledState(isDisabled: boolean) {
    this.disabled = isDisabled;
  }
}
