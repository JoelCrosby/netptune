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
  selector: 'app-form-textarea',
  templateUrl: './form-textarea.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormTextAreaComponent implements ControlValueAccessor {
  @Input() label!: string;
  @Input() disabled!: boolean;
  @Input() icon!: string;
  @Input() prefix!: string;
  @Input() placeholder: string | null = null;
  @Input() hint: string | null = null;
  @Input() minLength: string | null = null;
  @Input() maxLength: string | null = null;
  @Input() rows = '2';

  @ViewChild('input') input!: ElementRef;

  @Output() submitted = new EventEmitter<string>();

  value: string | number = '';

  onChange!: (value: string) => void;
  onTouch!: () => void;

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
