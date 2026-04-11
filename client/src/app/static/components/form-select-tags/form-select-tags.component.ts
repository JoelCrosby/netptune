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
  templateUrl: './form-select-tags.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [FormSelectTagsService],
  imports: [LucideDynamicIcon, LucideChevronDown, FormSelectDropdownComponent],
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
  readonly readonly = input<boolean>(false);
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
