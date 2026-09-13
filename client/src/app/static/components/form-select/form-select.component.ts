import { CdkPortal } from '@angular/cdk/portal';
import {
  Component,
  computed,
  contentChildren,
  ElementRef,
  inject,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import {
  FormValueControl,
  ValidationError,
  WithOptionalFieldTree,
} from '@angular/forms/signals';
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
import { describedByIds, errorIdFor, hintIdFor } from '../form-control-a11y';
import { FormSelectOptionComponent } from './form-select-option.component';
import { FormSelectDropdownStyleDirective } from './form-select.directives';
import { FormSelectService } from './form-select.service';
import { FormErrorComponent } from '../form-error/form-error.component';
import { ListboxKeyboard } from '../listbox-keyboard';

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
    FormErrorComponent,
  ],
  template: `
    <div
      class="nept-form-control mb-[1.4rem] w-[inherit]"
      [class.mb-0!]="noMargin()">
      @if (label()) {
        <label [for]="name()" appFormLabel>
          {{ label() }}
        </label>
      }

      <app-form-control-field
        #dropreference
        class="w-full cursor-pointer"
        [disabled]="disabled()"
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
          [attr.aria-invalid]="ariaInvalid()"
          [attr.aria-describedby]="describedBy()"
          readonly
          (click)="$event.stopPropagation(); showDropdown()"
          (keydown)="keyboard.handleKeydown($event)"
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
        <small [id]="hintId()" appFormHint>{{ hint() }}</small>
      }

      @if (showErrors()) {
        <div [id]="errorId()">
          @for (error of errors(); track error.kind) {
            <app-form-error>
              {{ error.message }}
            </app-form-error>
          }
        </div>
      }
    </div>
  `,
})
export class FormSelectComponent<
  TValue,
> implements FormValueControl<TValue | null> {
  private service = inject<FormSelectService<TValue>>(FormSelectService);

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
  readonly errors = input<readonly WithOptionalFieldTree<ValidationError>[]>(
    []
  );

  readonly showErrors = computed(
    () => this.touched() && this.errors().length > 0
  );
  readonly hintId = computed(() => hintIdFor(this.name()));
  readonly errorId = computed(() => errorIdFor(this.name()));
  readonly ariaInvalid = computed(() => (this.showErrors() ? 'true' : null));

  describedBy(): string | null {
    return describedByIds(this.name(), !!this.hint(), this.showErrors());
  }
  readonly pending = input<boolean>(false);
  readonly noMargin = input(false);

  selectedPortal?: CdkPortal;

  isOpen = computed(() => this.dropdown().showing());

  readonly selectedOption = computed(() => {
    const value = this.value();

    return this.options().find((option) => option.value() === value) ?? null;
  });

  readonly displayValue = computed(
    () => this.selectedOption()?.viewValue ?? ''
  );

  readonly keyboard = new ListboxKeyboard({
    items: this.options,
    isOpen: () => this.isOpen(),
    open: () => this.showDropdown(),
    close: () => this.hideDropdown(),
    select: (option) => this.selectOption(option),
    openKeys: ['Enter', ' ', 'ArrowDown', 'ArrowUp'],
    selectKeys: ['Enter', ' '],
    trapKeys: ['PageUp', 'PageDown', 'Tab'],
    horizontal: true,
    wrap: true,
  });

  constructor() {
    this.service.register(this);
  }

  showDropdown() {
    this.dropdown().show();

    if (!this.options()?.length) {
      return;
    }

    const selected = this.selectedOption();

    if (selected) {
      this.keyboard.setActiveItem(selected);
    } else {
      this.keyboard.setFirstItemActive();
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

    this.value.set(value ?? null);
    this.keyboard.setActiveItem(option);

    this.hideDropdown();

    if (value === undefined || value === null) return;

    this.changed.emit(value);
    this.input().nativeElement.focus();
  }
}
