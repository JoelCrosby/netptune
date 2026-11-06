import { Component, input, output } from '@angular/core';
import { LucidePlus } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

export type FieldKind =
  'select' | 'enum' | 'text' | 'date' | 'number' | 'tags' | 'people';

export const fieldKindLabels: Record<FieldKind, string> = {
  select: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:select`,
  enum: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:enum`,
  text: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:text`,
  date: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:date`,
  number: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:number`,
  tags: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:tags`,
  people: $localize`:Kind of control a field brings, shown beside its name in the add-a-field menu:people`,
};

export interface AddFieldMenuOption<TKey extends string> {
  key: TKey;
  label: string;
  kind: FieldKind;
}

@Component({
  selector: 'app-add-field-menu',
  imports: [DropdownMenuComponent, LucidePlus, MenuItemComponent],
  host: { class: 'relative block' },
  template: `
    <button
      #trigger
      type="button"
      class="border-foreground/24 text-primary hover:border-primary/50 hover:bg-primary/8 inline-flex h-10 cursor-pointer items-center gap-2 rounded-sm border border-dashed px-3.5 text-sm font-medium transition-colors disabled:pointer-events-none disabled:opacity-40"
      [disabled]="!options().length"
      (click)="menu.toggle(trigger)">
      <svg lucidePlus class="h-4 w-4" aria-hidden="true"></svg>
      <span i18n="Button that adds another field to an edit">
        Add a field
      </span>
    </button>

    <app-dropdown-menu #menu>
      @for (option of options(); track option.key) {
        <button
          app-menu-item
          type="button"
          (click)="added.emit(option.key); menu.close()">
          <span class="min-w-0 flex-1 truncate">{{ option.label }}</span>
          <span class="text-foreground/40 font-mono text-[11px]">
            {{ kindLabels[option.kind] }}
          </span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class AddFieldMenuComponent<TKey extends string> {
  protected readonly kindLabels = fieldKindLabels;

  readonly options = input.required<readonly AddFieldMenuOption<TKey>[]>();

  readonly added = output<TKey>();
}
