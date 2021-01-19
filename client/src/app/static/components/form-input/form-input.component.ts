import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  forwardRef,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  NG_VALUE_ACCESSOR,
} from '@angular/forms';

@Component({
  selector: 'app-form-input',
  templateUrl: './form-input.component.html',
  styleUrls: ['./form-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => FormInputComponent),
      multi: true,
    },
  ],
})
export class FormInputComponent implements OnInit, ControlValueAccessor {
  @Input() label: string;
  @Input() name: string;
  @Input() icon: string;
  @Input() prefix: string;
  @Input() autocomplete = 'off';
  @Input() placeholder = '';
  @Input() type: 'text' | 'number' = 'text';

  @Output() submitted = new EventEmitter<string>();

  formControl = new FormControl();

  onChange: (value: string) => void;
  onTouch: () => void;

  constructor() {}

  ngOnInit() {}

  onSubmit() {
    this.submitted.emit(this.formControl.value);
  }

  writeValue(obj: string) {
    this.formControl.setValue(obj);
  }

  registerOnChange(fn: (...args: unknown[]) => unknown) {
    this.onChange = fn;
  }

  registerOnTouched(fn: (...args: unknown[]) => unknown) {
    this.onTouch = fn;
  }

  setDisabledState?(isDisabled: boolean) {
    if (isDisabled) {
      this.formControl.enable();
    } else {
      this.formControl.disable();
    }
  }
}
