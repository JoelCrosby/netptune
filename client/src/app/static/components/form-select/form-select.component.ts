import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import { CdkPortal } from '@angular/cdk/portal';
import {
  Component,
  computed,
  contentChildren,
  ElementRef,
  inject,
  Injector,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';
import {
  LucideChevronDown,
  LucideDynamicIcon,
  LucideIconInput,
} from '@lucide/angular';
import { FormControlFieldComponent } from '../form-control/form-control-field.component';
import {
  FormControlHintDirective,
  FormControlInputDirective,
  FormControlLabelDirective,
  FormControlPrefixDirective,
} from '../form-control/form-control.directives';
import { FormSelectDropdownComponent } from './form-select-dropdown.component';
import { FormSelectOptionComponent } from './form-select-option.component';
import { FormSelectDropdownStyleDirective } from './form-select.directives';
import { FormSelectService } from './form-select.service';

@Component({
  selector: 'app-form-select',
  providers: [FormSelectService],
  imports: [
    LucideDynamicIcon,
    LucideChevronDown,
    FormSelectDropdownComponent,
    FormSelectDropdownStyleDirective,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormControlLabelDirective,
    FormControlHintDirective,
    FormControlPrefixDirective,
  ],
  template: `<div class="nept-form-control mb-[1.4rem] w-[inherit]">
    @if (label()) {
      <label [for]="name()" appFormLabel>
        {{ label() }}
      </label>
    }

    <app-form-control-field
      #dropreference
      class="w-full cursor-pointer"
      [invalid]="touched() && invalid()"
      [active]="!!value() && pending()"
      (click)="onDropMenuIconClick($event)">
      @if (prefix()) {
        <div appFormPrefix>{{ prefix() }}</div>
      }

      <input
        #input
        appFormInput
        [placeholder]="placeholder()"
        [id]="name()"
        [value]="displayValue()"
        [disabled]="disabled()"
        class="grow cursor-pointer selection:bg-transparent"
        [style.padding]="prefix() ? '0 .8rem 0 0' : '0 .8rem'"
        readonly
        (click)="$event.stopPropagation(); showDropdown()"
        (keydown)="onKeyDown($event)"
        (blur)="touched.set(true)"
        autocomplete="off" />

      <div
        class="hidden"
        [style.padding]="prefix() ? '0 .8rem 0 0' : '0 .8rem'"></div>

      <ng-content />

      @if (icon()) {
        <svg
          class="mr-3"
          [lucideIcon]="icon()!"
          size="20"
          aria-hidden="true"></svg>
      }

      <svg
        lucideChevronDown
        size="28"
        aria-hidden="true"
        class="text-foreground/70 mr-4 flex items-center justify-center"></svg>

      <app-form-select-dropdown [reference]="dropreference.el">
        <div appFormSelectDropdown class="menu-scale-in">
          <ng-content select="app-form-select-option" />
        </div>
      </app-form-select-dropdown>
    </app-form-control-field>

    @if (hint()) {
      <small appFormHint> {{ hint() }} </small>
    }
  </div> `,
})
export class FormSelectComponent<TValue>
  implements FormValueControl<TValue | null>
{
  private service = inject<FormSelectService<TValue>>(FormSelectService);
  private injector = inject(Injector);

  readonly label = input.required<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly prefix = input<string>();
  readonly placeholder = input<string>('');
  readonly hint = input<string>();

  readonly changed = output<TValue>();
  readonly input = viewChild.required<ElementRef>('input');
  readonly options = contentChildren<FormSelectOptionComponent<TValue>>(
    FormSelectOptionComponent
  );
  readonly submitted = output<string>();

  public readonly dropdown = viewChild.required(FormSelectDropdownComponent);

  readonly value = model<TValue | null>(null);
  readonly name = input<string>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly pending = input<boolean>(false);

  selectedPortal?: CdkPortal;
  keyManager?: ActiveDescendantKeyManager<FormSelectOptionComponent<TValue>>;

  isOpen = computed(() => this.dropdown().showing());

  readonly selectedOption = computed(() => {
    const value = this.value();

    return this.options().find((option) => option.value() === value) ?? null;
  });

  readonly displayValue = computed(() => this.selectedOption()?.viewValue ?? '');

  constructor() {
    this.service.register(this);

    this.keyManager = new ActiveDescendantKeyManager(
      this.options,
      this.injector
    )
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
  }

  hideDropdown() {
    this.dropdown().hide();
  }

  selectOption(option: FormSelectOptionComponent<TValue> | undefined | null) {
    if (!option) {
      this.value.set(null);

      return;
    }

    const value = option.value();

    this.value.set(value);
    this.keyManager?.setActiveItem(option);

    this.hideDropdown();

    if (value === undefined || value === null) return;

    this.changed.emit(value);
    this.input().nativeElement.focus();
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
