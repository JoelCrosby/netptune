import {
  ChangeDetectionStrategy,
  Component,
  Input,
  Optional,
  Self,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { colorDictionary, NamedColor } from '@core/util/colors/colors';

@Component({
  selector: 'app-color-select',
  templateUrl: './color-select.component.html',
  styleUrls: ['./color-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ColorSelectComponent implements ControlValueAccessor {
  @Input() label: string;
  @Input() disabled: boolean;
  @Input() hint: string = null;

  colors = colorDictionary();

  value: string | undefined;

  get control() {
    return this.ngControl.control;
  }

  onChange: (value: string) => void;
  onTouch: (...args: unknown[]) => void;

  constructor(
    @Self()
    @Optional()
    public ngControl: NgControl
  ) {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  onOptionClicked(color: NamedColor) {
    this.value = color.color;
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
