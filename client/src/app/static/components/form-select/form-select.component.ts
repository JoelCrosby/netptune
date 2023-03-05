import {
  CdkPortal,
  CdkPortalOutlet,
  ComponentPortal,
  TemplatePortal,
} from '@angular/cdk/portal';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  ElementRef,
  EventEmitter,
  Input,
  Optional,
  Output,
  QueryList,
  Self,
  TemplateRef,
  ViewChild,
  ViewContainerRef,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { FormSelectDropdownComponent } from './form-select-dropdown.component';
import { FormSelectOptionComponent } from './form-select-option.component';
import { FormSelectService } from './form-select.service';

@Component({
  selector: 'app-form-select',
  templateUrl: './form-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [FormSelectService],
})
export class FormSelectComponent
  implements AfterViewInit, ControlValueAccessor
{
  @Input() label!: string;
  @Input() disabled!: boolean;
  @Input() icon!: string;
  @Input() prefix!: string;
  @Input() autocomplete = 'off';
  @Input() placeholder?: string;
  @Input() hint?: string;
  @Input() minLength?: string;
  @Input() maxLength?: string;

  @ViewChild('input') input!: ElementRef;

  @ViewChild(FormSelectDropdownComponent)
  public dropdown!: FormSelectDropdownComponent;

  @ViewChild('placeholderPortal')
  placeholderPortalContent!: TemplateRef<unknown>;

  placeholderPortal?: TemplatePortal<unknown>;

  @ViewChild(CdkPortalOutlet)
  portalOutlet?: CdkPortalOutlet;

  @ContentChildren(FormSelectOptionComponent, { descendants: true })
  options?: QueryList<FormSelectOptionComponent>;

  @Output() submitted = new EventEmitter<string>();

  value?: unknown;
  displayValue: unknown | null = null;

  selectedPortal?: CdkPortal;

  onChange!: (value: string) => void;
  onTouch!: (...args: unknown[]) => void;

  selectedOption?: FormSelectOptionComponent | null = null;

  selectTrigger?: ComponentPortal<FormSelectOptionComponent>;

  get control() {
    return this.ngControl.control;
  }

  constructor(
    @Self()
    @Optional()
    public ngControl: NgControl,
    private viewContainerRef: ViewContainerRef,
    private service: FormSelectService
  ) {
    this.service.register(this);

    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  ngAfterViewInit() {
    this.selectTrigger = new ComponentPortal(
      FormSelectOptionComponent,
      this.viewContainerRef
    );

    this.placeholderPortal = new TemplatePortal(
      this.placeholderPortalContent,
      this.viewContainerRef
    );

    this.selectedPortal = this.placeholderPortal;
  }

  showDropdown() {
    this.dropdown.show();
  }

  onDropMenuIconClick(event: UIEvent) {
    event.stopPropagation();
    setTimeout(() => {
      this.input.nativeElement.focus();
      this.input.nativeElement.click();
    }, 10);
  }

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.onChange(value);
    this.onTouch();
  }

  hideDropdown() {
    this.dropdown.hide();
  }

  selectOption(option: FormSelectOptionComponent) {
    this.value = option.value;
    this.selectedOption = option;

    this.updateTrigger();
    this.hideDropdown();

    this.input.nativeElement.focus();

    this.selectedPortal?.attach(option);
  }

  writeValue(value: string) {
    this.value = value;

    if (!this.options) {
      return;
    }

    this.selectedOption = this.options
      .toArray()
      .find((option) => option.value === this.value);
  }

  updateTrigger() {
    this.displayValue = this.selectedOption ? this.selectedOption.value : null;
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
