import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
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
import { FormSelectDropdownComponent } from '../form-select/form-select-dropdown.component';
import { FormSelectTagsOptionComponent } from './form-select-tags-option.component';
import { FormSelectTagsService } from './form-select-tags.service';

@Component({
  selector: 'app-form-select-tags',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [FormSelectTagsService],
  imports: [LucideDynamicIcon, LucideChevronDown, FormSelectDropdownComponent],
  template: `<div class="nept-form-control">
    @if (label()) {
      <!-- eslint-disable @angular-eslint/template/label-has-associated-control -->
      <label class="form-control-label">{{ label() }}</label>
    }

    <div
      #dropreference
      class="form-control-input cursor-text! flex-wrap!"
      [class.invalid]="touched() && invalid()"
      [class.active]="value().length > 0 && pending()"
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
                aria-label="Remove">
                &times;
              </button>
            }
          </span>
        }

        <input
          #searchInput
          class="w-auto! min-w-15 flex-1"
          [placeholder]="selectedOptions().length === 0 ? placeholder() : ''"
          [disabled]="disabled()"
          [readOnly]="isReadonly()"
          (input)="onSearchInput($event)"
          (keydown)="onKeyDown($event)"
          (blur)="onBlur()"
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

      <app-form-select-dropdown [reference]="dropreference">
        <div class="form-select-dropdown menu-scale-in">
          <ng-content select="app-form-select-tags-option" />
        </div>
      </app-form-select-dropdown>
    </div>

    @if (hint()) {
      <small class="form-control-hint">{{ hint() }}</small>
    }
  </div> `,
})
export class FormSelectTagsComponent<TValue>
  implements AfterViewInit, FormValueControl<TValue[]>
{
  private service = inject<FormSelectTagsService<TValue>>(
    FormSelectTagsService
  );

  readonly label = input<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly placeholder = input<string>('');
  readonly hint = input<string>();

  readonly changed = output<TValue[]>();
  readonly searchInput = viewChild.required<ElementRef>('searchInput');
  readonly options = contentChildren(FormSelectTagsOptionComponent);
  readonly dropdown = viewChild.required(FormSelectDropdownComponent);

  readonly value = model<TValue[]>([]);
  readonly name = input<string>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly pending = input<boolean>(false);

  readonly searchQuery = signal<string>('');

  readonly selectedOptions = computed(() => {
    const values = this.value();
    return this.options().filter((opt) =>
      values.some((v) => v === opt.value())
    );
  });

  keyManager?: ActiveDescendantKeyManager<
    FormSelectTagsOptionComponent<TValue>
  >;

  constructor() {
    this.service.register(this);
  }

  ngAfterViewInit() {
    this.rebuildKeyManager();
  }

  isSelected(value: TValue): boolean {
    return this.value().some((v) => v === value);
  }

  private rebuildKeyManager() {
    const visibleOptions = this.options().filter((opt) => !opt.hiddenBySearch);
    this.keyManager = new ActiveDescendantKeyManager(visibleOptions)
      .withVerticalOrientation()
      .withWrap();
  }

  showDropdown() {
    this.dropdown().show();
    if (this.options()?.length) {
      this.keyManager?.setFirstItemActive();
    }
  }

  hideDropdown() {
    this.dropdown().hide();
    this.searchQuery.set('');
    this.searchInput().nativeElement.value = '';
  }

  toggleOption(option: FormSelectTagsOptionComponent<TValue>) {
    const val = option.value();
    const current = this.value();
    const isSelected = current.some((v) => v === val);

    this.value.set(
      isSelected ? current.filter((v) => v !== val) : [...current, val]
    );
    this.changed.emit(this.value());
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
    this.rebuildKeyManager();
    if (!this.dropdown().showing()) {
      this.showDropdown();
    }
  }

  onBlur() {
    this.touched.set(true);
  }

  onKeyDown(event: KeyboardEvent) {
    const dropdown = this.dropdown();

    if (event.key === 'Escape' || event.key === 'Esc') {
      if (dropdown.showing()) {
        this.hideDropdown();
        this.searchInput().nativeElement.focus();
      }
      return;
    }

    if (event.key === 'Backspace' && !this.searchQuery()) {
      const current = this.value();
      if (current.length) {
        this.value.set(current.slice(0, -1));
        this.changed.emit(this.value());
      }
      return;
    }

    if (!dropdown.showing()) {
      if (['ArrowDown', 'Down', 'ArrowUp', 'Up', 'Enter'].includes(event.key)) {
        this.showDropdown();
      }
      return;
    }

    if (event.key === 'Enter') {
      const activeItem = this.keyManager?.activeItem;
      if (activeItem) {
        this.toggleOption(activeItem);
      }
    } else if (['ArrowUp', 'Up', 'ArrowDown', 'Down'].includes(event.key)) {
      event.preventDefault();
      this.keyManager?.onKeydown(event);
    } else if (event.key === 'Tab') {
      event.preventDefault();
    }
  }
}
