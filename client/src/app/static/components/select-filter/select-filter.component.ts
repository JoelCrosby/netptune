import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { LucideIconInput } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';

export interface SelectFilterOption<T> {
  value: T;
  label: string;
}

const emptyValue = 'empty';

// Every option is namespaced so no option value can collide with the empty entry.
function optionValue(value: string | number): string {
  return `option:${value}`;
}

@Component({
  selector: 'app-select-filter',
  imports: [
    DropdownMenuComponent,
    FilterActionButtonComponent,
    FilterOptionListComponent,
  ],
  template: `
    <app-filter-action-button
      [label]="label()"
      [icon]="icon()"
      [color]="hasValue() ? 'primary' : undefined"
      [count]="hasValue() ? 1 : 0"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu panelRole="none" [panelClass]="'p-0'">
      <app-filter-option-list
        [open]="menu.showing()"
        [multiple]="false"
        [options]="listOptions()"
        [selected]="selectedValues()"
        [listAriaLabel]="label()"
        [searchPlaceholder]="searchPlaceholder() ?? defaultSearchPlaceholder"
        (toggled)="onToggled($event); menu.close()"
        (dismissed)="menu.closeAndFocusTrigger()" />
    </app-dropdown-menu>
  `,
})
export class SelectFilterComponent<T extends string | number> {
  readonly el = inject(ElementRef);

  readonly label = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly options = input<SelectFilterOption<T>[]>([]);
  readonly value = input<T | null>(null);
  /** The menu entry that stands for "no filter", e.g. "All entities". */
  readonly emptyLabel = input.required<string>();
  readonly searchPlaceholder = input<string | null>(null);

  readonly changed = output<T | null>();

  protected readonly defaultSearchPlaceholder = $localize`:Placeholder in the box that narrows a filter's options:Search`;

  protected readonly hasValue = computed(() => this.value() !== null);

  protected readonly listOptions = computed<FilterOption<string>[]>(() => {
    const empty: FilterOption<string> = {
      value: emptyValue,
      label: this.emptyLabel(),
      sticky: true,
    };

    const options = this.options().map((option) => ({
      value: optionValue(option.value),
      label: option.label,
    }));

    return [empty, ...options];
  });

  protected readonly selectedValues = computed(() => {
    const value = this.value();

    return new Set([value === null ? emptyValue : optionValue(value)]);
  });

  protected onToggled(value: string) {
    if (value === emptyValue) {
      this.changed.emit(null);

      return;
    }

    const option = this.options().find(
      (candidate) => optionValue(candidate.value) === value
    );

    this.changed.emit(option?.value ?? null);
  }
}
