import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import { CdkPortal } from '@angular/cdk/portal';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  inject,
  input,
  output,
  viewChild,
  contentChildren
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { FormSelectDropdownComponent } from './form-select-dropdown.component';
import { FormSelectOptionComponent } from './form-select-option.component';
import { FormSelectService } from './form-select.service';

import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-form-select',
  templateUrl: './form-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [FormSelectService],
  imports: [MatIcon, FormSelectDropdownComponent],
})
export class FormSelectComponent<TValue>
  implements AfterViewInit, ControlValueAccessor
{
  ngControl = inject(NgControl, { self: true, optional: true });
  private service = inject<FormSelectService<TValue>>(FormSelectService);

  @Input() label!: string;
  @Input() disabled!: boolean;
  @Input() icon!: string;
  @Input() prefix!: string;
  readonly placeholder = input<string>();
  @Input() hint?: string;

  readonly changed = output<TValue>();

  readonly input = viewChild.required<ElementRef>('input');

  public readonly dropdown = viewChild.required(FormSelectDropdownComponent);

  readonly options = contentChildren(FormSelectOptionComponent, { descendants: true });

  readonly submitted = output<string>();

  value?: TValue | null;
  displayValue: string | null = null;

  selectedPortal?: CdkPortal;

  onChange!: (value: TValue) => void;
  onTouch!: (...args: unknown[]) => void;

  selectedOption?: FormSelectOptionComponent<TValue> | null = null;

  keyManager?: ActiveDescendantKeyManager<FormSelectOptionComponent<TValue>>;

  get control() {
    return this.ngControl?.control;
  }

  constructor() {
    this.service.register(this);

    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  ngAfterViewInit() {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    this.keyManager = new ActiveDescendantKeyManager(this.options()!)
      .withHorizontalOrientation('ltr')
      .withVerticalOrientation()
      .withWrap();
  }

  showDropdown() {
    this.dropdown().show();

    if (!this.options()?.length) {
      return;
    }

    this.selectedOption
      ? this.keyManager?.setActiveItem(this.selectedOption)
      : this.keyManager?.setFirstItemActive();
  }

  onDropMenuIconClick(event: UIEvent) {
    event.stopPropagation();
    setTimeout(() => {
      this.input().nativeElement.focus();
      this.input().nativeElement.click();
    }, 10);

    this.onTouch();
  }

  hideDropdown() {
    this.dropdown().hide();
  }

  selectOption(option: FormSelectOptionComponent<TValue> | undefined | null) {
    if (!option) {
      this.value = null;
      this.selectedOption = null;

      return;
    }

    this.value = option.value();
    this.selectedOption = option;

    this.keyManager?.setActiveItem(option);

    this.updateTrigger();
    this.hideDropdown();

    const value = this.selectedOption?.value();
    this.changed.emit(value);
    this.onChange(value);

    this.input().nativeElement.focus();
  }

  writeValue(value: TValue) {
    this.value = value;

    const options = this.options();
    if (!options) {
      return;
    }

    this.selectedOption = options
      .find((option) => option.value() === this.value);

    this.updateTrigger();
  }

  updateTrigger() {
    this.displayValue = this.selectedOption
      ? this.selectedOption.viewValue
      : null;

    this.input().nativeElement.value = this.displayValue;
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

  onKeyDown(event: KeyboardEvent) {
    const inactiveKeys = ['Enter', ' ', 'ArrowDown', 'Down', 'ArrowUp', 'Up'];
    const arrowKeys = [
      'ArrowUp',
      'Up',
      'ArrowDown',
      'Down',
      'ArrowRight',
      'Right',
      'ArrowLeft',
      'Left',
    ];

    const dropdown = this.dropdown();
    if (inactiveKeys.includes(event.key)) {
      if (!dropdown.showing) {
        this.showDropdown();
        return;
      }

      if (!this.options()?.length) {
        event.preventDefault();
        return;
      }
    }

    if (event.key === 'Enter' || event.key === ' ') {
      this.selectOption(this.keyManager?.activeItem);
    } else if (event.key === 'Escape' || event.key === 'Esc') {
      dropdown.showing && this.hideDropdown();
    } else if (arrowKeys.includes(event.key)) {
      this.keyManager?.onKeydown(event);
    } else if (
      event.key === 'PageUp' ||
      event.key === 'PageDown' ||
      event.key === 'Tab'
    ) {
      dropdown.showing && event.preventDefault();
    }
  }
}
