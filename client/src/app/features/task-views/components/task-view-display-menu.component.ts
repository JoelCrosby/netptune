import { Component, computed, input, model } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  LucideArrowDownNarrowWide,
  LucideArrowUpNarrowWide,
  LucideChevronDown,
  LucideChevronUp,
  LucideColumns3,
  LucideDynamicIcon,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { reconcileColumnPreferences } from '@static/components/datatable/datatable-columns.util';
import {
  DatatableColumn,
  DatatableColumnPreference,
} from '@static/components/datatable/datatable.types';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskQueryField } from '../models/task-view.models';

interface ColumnRow {
  id: string;
  header: string;
  visible: boolean;
  isFirst: boolean;
  isLast: boolean;
}

@Component({
  selector: 'app-task-view-display-menu',
  imports: [
    DropdownButtonComponent,
    IconButtonComponent,
    MenuCheckboxItemComponent,
    MenuItemComponent,
    LucideDynamicIcon,
    LucideChevronDown,
    LucideChevronUp,
    LucideColumns3,
  ],
  host: { class: 'flex items-center gap-2' },
  template: `
    <app-dropdown-button
      xPosition="before"
      color="neutral"
      buttonClass="h-9"
      i18n-menuLabel="Heading of the sort menu"
      menuLabel="Sort by"
      [label]="sortLabel()">
      <svg
        buttonPrefix
        [lucideIcon]="sortIcon()"
        class="h-3.5 w-3.5 shrink-0"></svg>

      <button
        app-menu-item
        type="button"
        [class.text-primary]="!sortBy()"
        (click)="sortBy.set('')">
        <span i18n="Option leaving a view on its default sort">Default</span>
      </button>

      @for (field of sortableFields(); track field.key) {
        <button
          app-menu-item
          type="button"
          class="justify-between"
          [class.text-primary]="sortBy() === field.sortKey"
          (click)="sortBy.set(field.sortKey ?? '')">
          <span>{{ field.name }}</span>
          @if (sortBy() === field.sortKey) {
            <span class="text-primary text-xs">
              {{ isDescending() ? '↓' : '↑' }}
            </span>
          }
        </button>
      }

      <button app-menu-item type="button" (click)="toggleDirection()">
        @if (isDescending()) {
          <span i18n="Descending sort direction">Descending ↓</span>
        } @else {
          <span i18n="Ascending sort direction">Ascending ↑</span>
        }
      </button>
    </app-dropdown-button>

    <app-dropdown-button
      xPosition="before"
      color="neutral"
      panelRole="dialog"
      buttonClass="h-9"
      i18n-menuLabel="Heading of the view column picker"
      menuLabel="Columns"
      [label]="columnsLabel()">
      <svg buttonPrefix lucideColumns3 class="h-3.5 w-3.5 shrink-0"></svg>

      @for (row of rows(); track row.id) {
        <div class="flex items-center gap-1">
          <button
            app-menu-checkbox-item
            class="min-w-0 flex-1"
            [class.text-foreground/50]="!row.visible"
            [checked]="row.visible"
            (checkedChange)="onToggle(row.id, $event)">
            <span class="truncate">{{ row.header }}</span>
          </button>

          <button
            app-icon-button
            class="h-7 w-7 shrink-0 rounded-md"
            type="button"
            [disabled]="row.isFirst"
            i18n-aria-label="
              Accessible label for the button that moves a column earlier
            "
            aria-label="Move column up"
            (click)="onMove(row.id, -1)">
            <svg lucideChevronUp class="h-3.5 w-3.5"></svg>
          </button>

          <button
            app-icon-button
            class="h-7 w-7 shrink-0 rounded-md"
            type="button"
            [disabled]="row.isLast"
            i18n-aria-label="
              Accessible label for the button that moves a column later
            "
            aria-label="Move column down"
            (click)="onMove(row.id, 1)">
            <svg lucideChevronDown class="h-3.5 w-3.5"></svg>
          </button>
        </div>
      }
    </app-dropdown-button>
  `,
})
export class TaskViewDisplayMenuComponent {
  readonly columns = input.required<DatatableColumn<TaskViewModel>[]>();
  readonly sortableFields = input.required<TaskQueryField[]>();

  readonly preferences = model.required<DatatableColumnPreference[]>();
  readonly sortBy = model.required<string>();
  readonly sortDirection = model.required<string>();

  readonly isDescending = computed(() => this.sortDirection() !== 'asc');

  protected readonly sortIcon = computed(() => {
    return this.isDescending()
      ? LucideArrowDownNarrowWide
      : LucideArrowUpNarrowWide;
  });

  readonly sortLabel = computed(() => {
    const match = this.sortableFields().find(
      (field) => field.sortKey === this.sortBy()
    );

    return (
      match?.name ??
      $localize`:Option leaving a view on its default sort:Default`
    );
  });

  readonly visibleCount = computed(
    () => this.ordered().filter((preference) => preference.visible).length
  );

  protected readonly columnsLabel = computed(() => {
    const count = this.visibleCount();

    return $localize`:Toolbar button showing how many columns a view shows. COUNT is how many:${count}:COUNT: columns`;
  });

  readonly rows = computed<ColumnRow[]>(() => {
    const headers = new Map(
      this.columns().map((column) => [column.id, column.header])
    );
    const ordered = this.ordered();

    return ordered.map((preference, index) => ({
      id: preference.id,
      header: headers.get(preference.id) ?? preference.id,
      visible: preference.visible,
      isFirst: index === 0,
      isLast: index === ordered.length - 1,
    }));
  });

  toggleDirection() {
    this.sortDirection.set(this.isDescending() ? 'asc' : 'desc');
  }

  onToggle(columnId: string, visible: boolean) {
    const next = this.ordered().map((preference) => {
      return preference.id === columnId
        ? { ...preference, visible }
        : preference;
    });

    this.preferences.set(next);
  }

  onMove(columnId: string, offset: number) {
    const next = [...this.ordered()];
    const index = next.findIndex((preference) => preference.id === columnId);
    const target = index + offset;
    const isOutOfRange = index < 0 || target < 0 || target >= next.length;

    if (isOutOfRange) return;

    [next[index], next[target]] = [next[target], next[index]];

    this.preferences.set(next);
  }

  // A column added to the catalog since the view was saved is appended switched off, so the
  // picker lists everything the table can render without changing what the view shows.
  private ordered(): DatatableColumnPreference[] {
    return reconcileColumnPreferences(this.columns(), this.preferences(), {
      newColumnsVisible: false,
    });
  }
}
