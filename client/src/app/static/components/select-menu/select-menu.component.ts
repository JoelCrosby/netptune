import { Component, computed, input, output } from '@angular/core';
import {
  LucideCheck,
  LucideDynamicIcon,
  type LucideIconInput,
} from '@lucide/angular';
import { type FlatButtonColor } from '../button/button.variants';
import { DropdownButtonComponent } from '../dropdown-menu/dropdown-button.component';
import { type DropdownMenuXPosition } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';

export interface SelectMenuOption<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-select-menu',
  imports: [
    DropdownButtonComponent,
    LucideCheck,
    LucideDynamicIcon,
    MenuItemComponent,
  ],
  template: `
    <app-dropdown-button
      #menu
      [label]="selectedLabel()"
      [ariaLabel]="ariaLabel()"
      [color]="color()"
      [buttonClass]="buttonClass()"
      [xPosition]="xPosition()">
      <span buttonPrefix class="contents">
        @if (icon(); as buttonIcon) {
          <svg [lucideIcon]="buttonIcon" class="h-4 w-4 shrink-0"></svg>
        }
      </span>

      @for (option of options(); track $index) {
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="isSelected(option)"
          (click)="select(option, menu)">
          <span class="flex h-4 w-4 shrink-0 items-center justify-center">
            @if (isSelected(option)) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span class="truncate">{{ option.label }}</span>
        </button>
      }
    </app-dropdown-button>
  `,
})
export class SelectMenuComponent<T> {
  readonly options = input.required<readonly SelectMenuOption<T>[]>();
  readonly value = input.required<T>();
  readonly ariaLabel = input<string>();
  readonly icon = input<LucideIconInput>();
  readonly color = input<FlatButtonColor>('neutral');
  readonly buttonClass = input('');
  readonly xPosition = input<DropdownMenuXPosition>('after');

  readonly valueChange = output<T>();

  protected readonly selectedLabel = computed(() => {
    const value = this.value();
    const selected = this.options().find((option) => option.value === value);

    return selected?.label ?? this.options()[0]?.label ?? '';
  });

  protected isSelected(option: SelectMenuOption<T>): boolean {
    return option.value === this.value();
  }

  protected select(option: SelectMenuOption<T>, menu: DropdownButtonComponent) {
    this.valueChange.emit(option.value);
    menu.close();
  }
}
