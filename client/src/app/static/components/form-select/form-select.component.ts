import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import { CdkPortal } from '@angular/cdk/portal';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { FormSelectDropdownComponent } from './form-select-dropdown.component';
import { FormSelectOptionComponent } from './form-select-option.component';
import { FormSelectService } from './form-select.service';

@Component({
  selector: 'app-form-select',
  templateUrl: './form-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [FormSelectService],
  imports: [MatIcon, FormSelectDropdownComponent],
})
export class FormSelectComponent<TValue> implements AfterViewInit {
  private service = inject<FormSelectService<TValue>>(FormSelectService);

  readonly label = input.required<string>();
  readonly icon = input<string>();
  readonly prefix = input<string>();
  readonly placeholder = input<string>('');
  readonly hint = input<string>();

  readonly changed = output<TValue>();
  readonly input = viewChild.required<ElementRef>('input');
  readonly options = contentChildren(FormSelectOptionComponent);
  readonly submitted = output<string>();

  public readonly dropdown = viewChild.required(FormSelectDropdownComponent);

  readonly value = signal<TValue | null>(null);
  readonly name = input<string>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly readonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly pending = input<boolean>(false);

  displayValue = signal<string | null>('');

  selectedPortal?: CdkPortal;

  onChange!: (value: TValue) => void;
  onTouch!: (...args: unknown[]) => void;

  selectedOption = signal<FormSelectOptionComponent<TValue> | null>(null);
  keyManager?: ActiveDescendantKeyManager<FormSelectOptionComponent<TValue>>;

  constructor() {
    this.service.register(this);
  }

  ngAfterViewInit() {
    this.updateTrigger();

    this.keyManager = new ActiveDescendantKeyManager(this.options())
      .withHorizontalOrientation('ltr')
      .withVerticalOrientation()
      .withWrap();
  }

  showDropdown() {
    this.dropdown().show();

    if (!this.options()?.length) {
      return;
    }

    const selected = this.selectedOption();

    if (selected) {
      this.keyManager?.setActiveItem(selected);
    } else {
      this.keyManager?.setFirstItemActive();
    }
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
      this.value.set(null);
      this.selectedOption.set(null);

      return;
    }

    this.value.set(option.value());
    this.selectedOption.set(option);

    this.keyManager?.setActiveItem(option);

    this.updateTrigger();
    this.hideDropdown();

    const value = this.selectedOption()?.value();

    if (!value) return;

    this.changed.emit(value);
    this.onChange(value);

    this.input().nativeElement.focus();
  }

  updateTrigger() {
    const options = this.options();

    if (!options.length) {
      return;
    }

    const selected = options.find((option) => option.value() === this.value());
    const display = selected ? selected.viewValue : null;
    this.displayValue.set(display);

    this.input().nativeElement.value = this.displayValue();
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
      if (dropdown.showing()) {
        this.hideDropdown();
      }
    } else if (arrowKeys.includes(event.key)) {
      this.keyManager?.onKeydown(event);
    } else if (
      event.key === 'PageUp' ||
      event.key === 'PageDown' ||
      event.key === 'Tab'
    ) {
      if (dropdown.showing()) {
        event.preventDefault();
      }
    }
  }
}
