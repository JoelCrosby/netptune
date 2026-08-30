import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { Status } from '@core/models/status';
import { LucideCircleDashed } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';

@Component({
  selector: 'app-status-filter',
  imports: [
    FilterActionButtonComponent,
    DropdownMenuComponent,
    FilterOptionListComponent,
  ],
  template: `
    <app-filter-action-button
      i18n-label="Label on the control that filters tasks by status"
      label="Filter by Status"
      [icon]="lucideCircleDashed"
      [color]="selectedCount() ? 'primary' : undefined"
      [count]="selectedCount()"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu panelRole="none" [panelClass]="'p-0'">
      <app-filter-option-list
        [open]="menu.showing()"
        [options]="options()"
        [selected]="selected()"
        i18n-searchPlaceholder="Placeholder in the box that searches statuses"
        searchPlaceholder="Search statuses"
        i18n-listAriaLabel="Accessible name of the status filter's option list"
        listAriaLabel="Statuses"
        i18n-emptyMessage="Shown when there are no statuses to filter by"
        emptyMessage="No statuses"
        (toggled)="toggled.emit($event)"
        (dismissed)="menu.closeAndFocusTrigger()" />
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

  protected readonly options = computed<FilterOption<number>[]>(() => {
    return this.statuses().map((status) => ({
      value: status.id,
      label: status.name,
    }));
  });
}
