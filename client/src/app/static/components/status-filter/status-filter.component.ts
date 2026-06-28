import { Component, ElementRef, inject, input, output } from '@angular/core';
import { Status } from '@core/models/status';
import { LucideCircleDashed } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';

@Component({
  selector: 'app-status-filter',
  imports: [
    FilterActionButtonComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
  ],
  template: `
    <app-filter-action-button
      label="Filter by Status"
      [icon]="lucideCircleDashed"
      [color]="selectedCount() ? 'primary' : undefined"
      [count]="selectedCount()"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      @for (status of statuses(); track status.id) {
        <button
          app-menu-checkbox-item
          [checked]="selected().has(status.id)"
          (checkedChange)="toggled.emit(status.id)">
          {{ status.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class StatusFilterComponent {
  readonly el = inject(ElementRef);

  readonly lucideCircleDashed = LucideCircleDashed;

  readonly statuses = input<Status[]>([]);
  readonly selected = input<Set<number>>(new Set());
  readonly selectedCount = input(0);

  readonly toggled = output<number>();
}
