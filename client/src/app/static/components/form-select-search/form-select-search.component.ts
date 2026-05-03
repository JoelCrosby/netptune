import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';
import { NgTemplateOutlet } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChild,
  ElementRef,
  TemplateRef,
  TrackByFunction,
  computed,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';
import {
  LucideChevronDown,
  LucideChevronRight,
  LucideDynamicIcon,
  LucideIconInput,
} from '@lucide/angular';
import { FormSelectDropdownComponent } from '../form-select/form-select-dropdown.component';

interface OptionTemplateContext<TOption> {
  $implicit: TOption;
  active: boolean;
  selected: boolean;
}

@Component({
  selector: 'app-form-select-search',
  templateUrl: './form-select-search.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NgTemplateOutlet,
    LucideDynamicIcon,
    LucideChevronDown,
    LucideChevronRight,
    FormSelectDropdownComponent,
  ],
})
export class FormSelectSearchComponent<TOption, TValue = TOption>
  implements AfterViewInit, FormValueControl<TValue | null>
{
  readonly label = input.required<string>();
  readonly options = input.required<readonly TOption[]>();
  readonly labelWith = input<(option: TOption) => string>((option) =>
    String(option ?? '')
  );
  readonly valueWith = input<(option: TOption) => TValue>(
    (option) => option as unknown as TValue
  );
  readonly compareWith = input<(a: TValue | null, b: TValue | null) => boolean>(
    (a, b) => a === b
  );
  readonly icon = input<LucideIconInput | null>();
  readonly prefix = input<string>();
  readonly placeholder = input<string>('');
  readonly hint = input<string>();
  readonly emptyMessage = input<string>('No options found');
  readonly itemSize = input<number>(40);
  readonly maxDropdownHeight = input<number>(288);

  readonly changed = output<TValue>();
  readonly optionSelected = output<TOption>();
  readonly input = viewChild.required<ElementRef<HTMLInputElement>>('input');
  readonly dropdown = viewChild.required(FormSelectDropdownComponent);
  readonly viewport = viewChild(CdkVirtualScrollViewport);

  readonly value = model<TValue | null>(null);
  readonly name = input<string>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly pending = input<boolean>(false);

  readonly searchQuery = signal('');
  readonly activeIndex = signal(0);
  readonly listboxId = `form-select-search-${crypto.randomUUID()}`;

  @ContentChild('option')
  optionTemplate?: TemplateRef<OptionTemplateContext<TOption>>;

  readonly isOpen = computed(() => this.dropdown().showing());

  readonly filteredOptions = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const options = this.options();

    if (!query) {
      return [...options];
    }

    return options.filter((option) =>
      this.labelWith()(option).toLowerCase().includes(query)
    );
  });

  readonly selectedOption = computed(() => {
    const value = this.value();
    return (
      this.options().find((option) =>
        this.compareWith()(this.valueWith()(option), value)
      ) ?? null
    );
  });

  readonly inputValue = computed(() => {
    if (this.isOpen()) {
      return this.searchQuery();
    }

    const selected = this.selectedOption();
    return selected ? this.labelWith()(selected) : '';
  });

  readonly dropdownHeight = computed(() => {
    const optionCount = this.filteredOptions().length;
    const contentHeight = Math.max(1, optionCount) * this.itemSize();
    return Math.min(contentHeight, this.maxDropdownHeight());
  });

  readonly activeOptionId = computed(() => {
    if (!this.isOpen() || !this.filteredOptions().length) {
      return null;
    }

    return this.optionId(this.activeIndex());
  });

  readonly trackByOption: TrackByFunction<TOption> = (_, option) =>
    this.valueWith()(option) ?? option;

  ngAfterViewInit() {
    this.setActiveToSelectedOption();
  }

  showDropdown() {
    if (this.disabled() || this.isReadonly()) {
      return;
    }

    if (!this.dropdown().showing()) {
      this.searchQuery.set('');
      this.dropdown().show();
    }

    this.setActiveToSelectedOption();
    this.scrollActiveOptionIntoView();
  }

  hideDropdown() {
    this.dropdown().hide();
    this.searchQuery.set('');
  }

  onTriggerClick(event: UIEvent) {
    event.stopPropagation();
    this.input().nativeElement.focus();
    this.showDropdown();
  }

  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
    this.activeIndex.set(0);

    if (!this.dropdown().showing()) {
      this.dropdown().show();
    }

    this.scrollActiveOptionIntoView();
  }

  onKeyDown(event: KeyboardEvent) {
    const dropdown = this.dropdown();

    if (event.key === 'Escape' || event.key === 'Esc') {
      if (dropdown.showing()) {
        event.preventDefault();
        this.hideDropdown();
      }
      return;
    }

    if (!dropdown.showing()) {
      if (
        ['ArrowDown', 'Down', 'ArrowUp', 'Up', 'Enter', ' '].includes(event.key)
      ) {
        event.preventDefault();
        this.showDropdown();
      }
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      this.selectActiveOption();
    } else if (event.key === 'ArrowDown' || event.key === 'Down') {
      event.preventDefault();
      this.moveActiveIndex(1);
    } else if (event.key === 'ArrowUp' || event.key === 'Up') {
      event.preventDefault();
      this.moveActiveIndex(-1);
    } else if (event.key === 'Home') {
      event.preventDefault();
      this.setActiveIndex(0);
      this.scrollActiveOptionIntoView();
    } else if (event.key === 'End') {
      event.preventDefault();
      this.setActiveIndex(this.filteredOptions().length - 1);
      this.scrollActiveOptionIntoView();
    } else if (event.key === 'Tab') {
      this.hideDropdown();
    }
  }

  selectOption(option: TOption, event?: UIEvent) {
    event?.preventDefault();
    event?.stopPropagation();

    const value = this.valueWith()(option);
    this.value.set(value);
    this.changed.emit(value);
    this.optionSelected.emit(option);
    this.hideDropdown();
    this.input().nativeElement.focus();
  }

  selectActiveOption() {
    const option = this.filteredOptions()[this.activeIndex()];

    if (option) {
      this.selectOption(option);
    }
  }

  isSelected(option: TOption): boolean {
    return this.compareWith()(this.valueWith()(option), this.value());
  }

  isActive(index: number): boolean {
    return this.activeIndex() === index;
  }

  setActiveIndex(index: number) {
    const maxIndex = this.filteredOptions().length - 1;
    this.activeIndex.set(Math.max(0, Math.min(index, maxIndex)));
  }

  optionId(index: number): string {
    return `${this.listboxId}-option-${index}`;
  }

  private moveActiveIndex(delta: number) {
    const options = this.filteredOptions();

    if (!options.length) {
      return;
    }

    const nextIndex =
      (this.activeIndex() + delta + options.length) % options.length;
    this.activeIndex.set(nextIndex);
    this.scrollActiveOptionIntoView();
  }

  private setActiveToSelectedOption() {
    const selected = this.selectedOption();
    const options = this.filteredOptions();
    const selectedIndex = selected ? options.indexOf(selected) : -1;
    this.activeIndex.set(selectedIndex >= 0 ? selectedIndex : 0);
  }

  private scrollActiveOptionIntoView() {
    queueMicrotask(() => {
      this.viewport()?.scrollToIndex(this.activeIndex());
    });
  }
}
