import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-form-input',
  templateUrl: './form-input.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon],
})
export class FormInputComponent implements ControlValueAccessor {
  ngControl = inject(NgControl, { self: true, optional: true });

  readonly label = input<string>();
  readonly disabled = model<boolean>();
  readonly icon = input<string | null>();
  readonly prefix = input<string | null>();
  readonly autocomplete = input('off');
  readonly placeholder = input<string | null>();
  readonly hint = input<string | null>();
  readonly minLength = input<string | null>();
  readonly maxLength = input<string | null>();
  readonly loading = input<boolean | null>(false);
  readonly type = input<'text' | 'number' | 'email' | 'password'>('text');

  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();

  value: string | number | null | undefined = '';

  onChange!: (value: string) => void;
  onTouch!: (...args: unknown[]) => void;

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
    if (value === null || value === undefined) {
      this.value = '';
      const inputValue = this.input();
      if (inputValue) {
        inputValue.nativeElement.value = '';
      }
    } else {
      this.value = value;
    }
  }

  registerOnChange(fn: (...args: unknown[]) => unknown) {
    this.onChange = fn;
  }

  registerOnTouched(fn: (...args: unknown[]) => unknown) {
    this.onTouch = fn;
  }

  setDisabledState(isDisabled: boolean) {
    this.disabled.set(isDisabled);
  }
}
