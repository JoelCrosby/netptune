import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { LucideCheck, LucideIconInput } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';

export interface SelectFilterOption<T> {
  value: T;
  label: string;
}

@Component({
  selector: 'app-select-filter',
  imports: [
    DropdownMenuComponent,
    FilterActionButtonComponent,
    LucideCheck,
    MenuItemComponent,
  ],
  template: `
    <app-filter-action-button
      [label]="label()"
      [icon]="icon()"
      [color]="hasValue() ? 'primary' : undefined"
      [count]="hasValue() ? 1 : 0"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      <button
        app-menu-item
        type="button"
        role="menuitemradio"
        [attr.aria-checked]="!hasValue()"
        (click)="select(null); menu.close()">
        <span class="flex h-4 w-4 items-center justify-center">
          @if (!hasValue()) {
            <svg lucideCheck class="h-4 w-4"></svg>
          }
        </span>
        <span>{{ emptyLabel() }}</span>
      </button>

      <div
        class="my-1 border-t border-neutral-200 dark:border-neutral-700"></div>

      <div class="custom-scroll max-h-72 overflow-y-auto">
        @for (option of options(); track option.value) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="value() === option.value"
            (click)="select(option.value); menu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (value() === option.value) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span class="max-w-52 truncate">{{ option.label }}</span>
          </button>
        }
      </div>
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

  readonly changed = output<T | null>();

  protected readonly hasValue = computed(() => this.value() !== null);

  protected select(value: T | null) {
    this.changed.emit(value);
  }
}
