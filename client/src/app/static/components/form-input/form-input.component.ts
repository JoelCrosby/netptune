import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Optional,
  Output,
  Self,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

@Component({
    selector: 'app-form-input',
    templateUrl: './form-input.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class FormInputComponent implements ControlValueAccessor {
  @Input() label!: string;
  @Input() disabled?: boolean;
  @Input() icon?: string | null;
  @Input() prefix?: string | null;
  @Input() autocomplete = 'off';
  @Input() placeholder?: string | null;
  @Input() hint?: string | null;
  @Input() minLength?: string | null;
  @Input() maxLength?: string | null;
  @Input() loading: boolean | null = false;
  @Input() type: 'text' | 'number' | 'email' | 'password' = 'text';

  @ViewChild('input') input!: ElementRef;

  @Output() submitted = new EventEmitter<string>();

  value: string | number | null | undefined = '';

  onChange!: (value: string) => void;
  onTouch!: (...args: unknown[]) => void;

  get control() {
    return this.ngControl.control;
  }

  constructor(
    @Self()
    @Optional()
    public ngControl: NgControl
  ) {
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
      if (this.input) {
        this.input.nativeElement.value = '';
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
    this.disabled = isDisabled;
  }
}
