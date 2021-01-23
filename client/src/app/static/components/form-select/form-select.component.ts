import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnInit,
  Optional,
  Output,
  Self,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

interface SelectOption {
  [key: string]: unknown;
}

@Component({
  selector: 'app-form-select',
  templateUrl: './form-select.component.html',
  styleUrls: ['./form-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectComponent implements OnInit, ControlValueAccessor {
  @Input() label: string;
  @Input() disabled: boolean;
  @Input() icon: string;
  @Input() prefix: string;
  @Input() autocomplete = 'off';
  @Input() placeholder: string = null;
  @Input() hint: string = null;
  @Input() minLength: string = null;
  @Input() maxLength: string = null;

  @Input() options: SelectOption[] = [];
  @Input() model: SelectOption;
  @Input() labelKey: string;
  @Input() idKey: string;

  @ViewChild('input') input: ElementRef;

  @Output() submitted = new EventEmitter<string>();

  value: string | number = '';

  onChange: (value: string) => void;
  onTouch: (...args: unknown[]) => void;

  get control() {
    return this.ngControl.control;
  }

  get selectLabel() {
    return this.model ? this.model[this.labelKey] : this.label;
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

  ngOnInit() {
    if (this.model !== undefined) {
      this.model = this.options.find(
        (currentOption) => currentOption[this.idKey] === this.model
      );
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
