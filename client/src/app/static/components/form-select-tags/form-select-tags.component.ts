import {
  Component,
  computed,
  contentChildren,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
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
} from '../form-control/form-control.directives';
import { FormSelectDropdownComponent } from '../form-select/form-select-dropdown.component';
import { FormSelectDropdownStyleDirective } from '../form-select/form-select.directives';
import { hintIdFor } from '../form-control-a11y';
import { ListboxKeyboard } from '../listbox-keyboard';
import { FormSelectTagsOptionComponent } from './form-select-tags-option.component';
import { FormSelectTagsService } from './form-select-tags.service';

@Component({
  selector: 'app-form-select-tags',
  providers: [FormSelectTagsService],
  imports: [
    LucideDynamicIcon,
    LucideChevronDown,
    FormSelectDropdownComponent,
    FormSelectDropdownStyleDirective,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormControlLabelDirective,
    FormControlHintDirective,
  ],
  template: `
    <div class="nept-form-control mb-[1.4rem] w-[inherit]">
      @if (label()) {
        <!-- eslint-disable @angular-eslint/template/label-has-associated-control -->
        <label appFormLabel>{{ label() }}</label>
      }

      <app-form-control-field
        #dropreference
        class="cursor-text! flex-wrap!"
        [disabled]="disabled()"
        [invalid]="touched() && invalid()"
        [active]="value().length > 0 && pending()"
        (click)="!isReadonly() && onTriggerClick($event)">
        <div class="flex min-w-0 flex-1 flex-wrap items-center gap-1 px-3 py-1">
          @for (option of selectedOptions(); track option.value()) {
            <span
              class="bg-primary-selected/40 inline-flex items-center gap-2 rounded-sm px-1.5 py-0.5 font-medium whitespace-nowrap">
              {{ option.viewValue }}

              @if (!isReadonly()) {
                <button
                  type="button"
                  class="cursor-pointer border-0! bg-transparent! p-0! text-sm leading-none text-inherit opacity-70 hover:opacity-100"
                  (click)="removeValue(option.value(), $event)"
                  i18n-aria-label="
                    Accessible label for the button that removes a selected tag
                  "
                  aria-label="Remove">
                  &times;
                </button>
              }
            </span>
          }

          <input
            #searchInput
            appFormInput
            class="w-auto! min-w-15 flex-1"
            [placeholder]="selectedOptions().length === 0 ? placeholder() : ''"
            [disabled]="disabled()"
            [readOnly]="isReadonly()"
            (input)="onSearchInput($event)"
            (keydown)="onKeyDown($event)"
            (blur)="onBlur()"
            [attr.aria-describedby]="hint() ? hintId() : null"
            autocomplete="off" />
        </div>

        @if (icon()) {
          <svg
            [lucideIcon]="icon()!"
            class="mr-3"
            size="20"
            aria-hidden="true"></svg>
        }

        @if (!isReadonly()) {
          <svg
            lucideChevronDown
            size="20"
            aria-hidden="true"
            class="mr-3 flex! items-center justify-center"></svg>
        }

        <app-form-select-dropdown [reference]="dropreference.el">
          <div appFormSelectDropdown class="menu-scale-in">
            <ng-content select="app-form-select-tags-option" />
          </div>
        </app-form-select-dropdown>
      </app-form-control-field>

      @if (hint()) {
        <small [id]="hintId()" appFormHint>{{ hint() }}</small>
      }
    </div>
  `,
})
export class FormSelectTagsComponent<TValue> implements FormValueControl<
  TValue[]
> {
  private service = inject<FormSelectTagsService<TValue>>(
    FormSelectTagsService
  );

  readonly label = input<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly placeholder = input<string>('');
  readonly hint = input<string>();

  readonly changed = output<TValue[]>();
  readonly searchInput = viewChild.required<ElementRef>('searchInput');
  readonly options = contentChildren<FormSelectTagsOptionComponent<TValue>>(
    FormSelectTagsOptionComponent
  );
  readonly dropdown = viewChild.required(FormSelectDropdownComponent);

  readonly value = model<TValue[]>([]);
  readonly name = input<string>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly hintId = computed(() => hintIdFor(this.name()));
  readonly pending = input<boolean>(false);

  readonly searchQuery = signal<string>('');

  readonly selectedOptions = computed(() => {
    const values = this.value();
    return this.options().filter((opt) =>
      values.some((v) => v === opt.value())
    );
  });

  readonly keyboard = new ListboxKeyboard({
    items: this.options,
    isOpen: () => this.dropdown().showing(),
    open: () => this.showDropdown(),
    close: () => {
      this.hideDropdown();
      this.searchInput().nativeElement.focus();
    },
    select: (option) => this.toggleOption(option),
    openKeys: ['ArrowDown', 'ArrowUp', 'Enter'],
    trapKeys: ['Tab'],
    skip: (option) => option.hiddenBySearch,
    wrap: true,
  });

  constructor() {
    this.service.register(this);
  }

  isSelected(value: TValue): boolean {
    return this.value().some((v) => v === value);
  }

  private clearSearch() {
    this.searchQuery.set('');
    this.searchInput().nativeElement.value = '';
    this.keyboard.clearActive();
  }

  showDropdown() {
    this.dropdown().show();
    if (this.options()?.length) {
      this.keyboard.setFirstItemActive();
    }
  }

  hideDropdown() {
    this.dropdown().hide();
    this.clearSearch();
  }

  toggleOption(option: FormSelectTagsOptionComponent<TValue>) {
    const val = option.value();
    const current = this.value();
    const isSelected = current.some((v) => v === val);

    this.value.set(
      isSelected ? current.filter((v) => v !== val) : [...current, val]
    );
    this.changed.emit(this.value());
    this.clearSearch();

    if (this.dropdown().showing()) {
      this.keyboard.setFirstItemActive();
    }

    this.searchInput().nativeElement.focus();
  }

  removeValue(value: TValue, event: UIEvent) {
    event.stopPropagation();
    this.value.set(this.value().filter((v) => v !== value));
    this.changed.emit(this.value());
  }

  onTriggerClick(event: UIEvent) {
    event.stopPropagation();
    this.searchInput().nativeElement.focus();
    if (!this.dropdown().showing()) {
      this.showDropdown();
    }
  }

  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
    this.keyboard.clearActive();
    if (!this.dropdown().showing()) {
      this.showDropdown();
    }
  }

  onBlur() {
    this.touched.set(true);
  }

  onKeyDown(event: KeyboardEvent) {
    const removesLastTag = event.key === 'Backspace' && !this.searchQuery();

    if (!removesLastTag) {
      this.keyboard.handleKeydown(event);

      return;
    }

    const current = this.value();

    if (current.length) {
      this.value.set(current.slice(0, -1));
      this.changed.emit(this.value());
    }
  }
}
