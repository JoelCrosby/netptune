import { Component, computed, input, output } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  LucideArrowDownNarrowWide,
  LucideArrowUpNarrowWide,
  LucideCheck,
  LucideChevronDown,
  LucideChevronUp,
  LucideColumns3,
} from '@lucide/angular';
import {
  DatatableColumn,
  DatatableColumnPreference,
} from '@static/components/datatable/datatable.types';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
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
    DropdownMenuComponent,
    MenuItemComponent,
    LucideArrowDownNarrowWide,
    LucideArrowUpNarrowWide,
    LucideCheck,
    LucideChevronDown,
    LucideChevronUp,
    LucideColumns3,
  ],
  host: { class: 'flex items-center gap-2' },
  template: `
    <button
      #sortOrigin
      type="button"
      class="border-foreground/10 text-foreground/70 hover:bg-foreground/5 hover:text-foreground flex h-[34px] items-center gap-2 rounded-lg border px-3 text-[13px] whitespace-nowrap transition-colors"
      [attr.aria-expanded]="sortMenu.showing()"
      (click)="sortMenu.toggle(sortOrigin)">
      @if (isDescending()) {
        <svg lucideArrowDownNarrowWide class="h-3.5 w-3.5"></svg>
      } @else {
        <svg lucideArrowUpNarrowWide class="h-3.5 w-3.5"></svg>
      }
      <span>{{ sortLabel() }}</span>
    </button>

    <app-dropdown-menu xPosition="before" #sortMenu>
      <div class="w-57.5">
        <p
          class="text-foreground/40 mx-2 mt-1.5 mb-2 text-[11px] font-semibold tracking-[0.06em] uppercase"
          i18n="Heading of the sort menu">
          Sort by
        </p>

        <button
          app-menu-item
          type="button"
          [class.text-primary]="!sortBy()"
          (click)="sortByChange.emit('')">
          <span i18n="Option leaving a view on its default sort">Default</span>
        </button>

        @for (field of sortableFields(); track field.key) {
          <button
            app-menu-item
            type="button"
            class="justify-between"
            [class.text-primary]="sortBy() === field.sortKey"
            (click)="sortByChange.emit(field.sortKey ?? '')">
            <span>{{ field.name }}</span>
            @if (sortBy() === field.sortKey) {
              <span class="text-primary text-xs">
                {{ isDescending() ? '↓' : '↑' }}
              </span>
            }
          </button>
        }

        <div class="bg-foreground/10 mx-1 my-2 h-px"></div>

        <button app-menu-item type="button" (click)="toggleDirection()">
          @if (isDescending()) {
            <span i18n="Descending sort direction">Descending ↓</span>
          } @else {
            <span i18n="Ascending sort direction">Ascending ↑</span>
          }
        </button>
      </div>
    </app-dropdown-menu>

    <button
      #columnsOrigin
      type="button"
      class="border-foreground/10 text-foreground/70 hover:bg-foreground/5 hover:text-foreground flex h-[34px] items-center gap-2 rounded-lg border px-3 text-[13px] whitespace-nowrap transition-colors"
      [attr.aria-expanded]="columnsMenu.showing()"
      (click)="columnsMenu.toggle(columnsOrigin)">
      <svg lucideColumns3 class="h-3.5 w-3.5"></svg>
      <span i18n="Toolbar button showing how many columns a view shows">
        {{ visibleCount() }} columns
      </span>
    </button>

    <app-dropdown-menu xPosition="before" panelRole="dialog" #columnsMenu>
      <div class="max-h-[420px] w-[268px] overflow-y-auto">
        <p
          class="text-foreground/40 mx-2 mt-1.5 mb-2 text-[11px] font-semibold tracking-[0.06em] uppercase"
          i18n="Heading of the view column picker">
          Columns
        </p>

        @for (row of rows(); track row.id) {
          <div
            class="hover:bg-foreground/5 flex h-[34px] items-center gap-1.5 rounded-sm pr-1 pl-2 transition-colors">
            <button
              type="button"
              class="flex h-full flex-1 cursor-pointer items-center gap-2.5 text-left text-sm"
              [class.text-foreground/50]="!row.visible"
              role="menuitemcheckbox"
              [attr.aria-checked]="row.visible"
              (click)="onToggle(row.id, !row.visible)">
              <span
                class="flex h-[17px] w-[17px] shrink-0 items-center justify-center rounded-[5px] border-2 transition-colors"
                [class.border-primary]="row.visible"
                [class.bg-primary]="row.visible"
                [class.border-foreground/30]="!row.visible">
                @if (row.visible) {
                  <svg
                    lucideCheck
                    strokeWidth="4"
                    class="text-primary-foreground h-3 w-3"></svg>
                }
              </span>
              <span>{{ row.header }}</span>
            </button>

            <button
              type="button"
              class="text-foreground/35 hover:text-foreground hover:bg-foreground/8 flex h-[26px] w-[22px] items-center justify-center rounded-md transition-colors disabled:pointer-events-none disabled:opacity-30"
              [disabled]="row.isFirst"
              i18n-aria-label="
                Accessible label for the button that moves a column earlier
              "
              aria-label="Move column up"
              (click)="onMove(row.id, -1)">
              <svg lucideChevronUp class="h-3.5 w-3.5"></svg>
            </button>

            <button
              type="button"
              class="text-foreground/35 hover:text-foreground hover:bg-foreground/8 flex h-[26px] w-[22px] items-center justify-center rounded-md transition-colors disabled:pointer-events-none disabled:opacity-30"
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
      </div>
    </app-dropdown-menu>
  `,
})
export class TaskViewDisplayMenuComponent {
  readonly columns = input.required<DatatableColumn<TaskViewModel>[]>();
  readonly preferences = input.required<DatatableColumnPreference[]>();
  readonly sortableFields = input.required<TaskQueryField[]>();
  readonly sortBy = input.required<string>();
  readonly sortDirection = input.required<string>();

  readonly preferencesChange = output<DatatableColumnPreference[]>();
  readonly sortByChange = output<string>();
  readonly sortDirectionChange = output<string>();

  readonly isDescending = computed(() => this.sortDirection() !== 'asc');

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
    this.sortDirectionChange.emit(this.isDescending() ? 'asc' : 'desc');
  }

  onToggle(columnId: string, visible: boolean) {
    const next = this.ordered().map((preference) => {
      return preference.id === columnId
        ? { ...preference, visible }
        : preference;
    });

    this.preferencesChange.emit(next);
  }

  onMove(columnId: string, offset: number) {
    const next = [...this.ordered()];
    const index = next.findIndex((preference) => preference.id === columnId);
    const target = index + offset;
    const isOutOfRange = index < 0 || target < 0 || target >= next.length;

    if (isOutOfRange) return;

    [next[index], next[target]] = [next[target], next[index]];

    this.preferencesChange.emit(next);
  }

  // Columns added to the catalog since the view was saved are appended rather than dropped, so the
  // picker always lists everything the table can render.
  private ordered(): DatatableColumnPreference[] {
    const preferences = this.preferences();
    const seen = new Set(preferences.map((preference) => preference.id));
    const added = this.columns()
      .filter((column) => !seen.has(column.id))
      .map((column) => ({ id: column.id, visible: false }));

    return [...preferences, ...added];
  }
}
