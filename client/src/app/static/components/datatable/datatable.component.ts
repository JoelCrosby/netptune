import { NgComponentOutlet, NgTemplateOutlet } from '@angular/common';
import { httpResource } from '@angular/common/http';
import {
  Component,
  Injector,
  OnDestroy,
  booleanAttribute,
  computed,
  contentChild,
  contentChildren,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { Params } from '@angular/router';
import { ClientResponse } from '@app/core/models/client-response';
import { Page } from '@app/core/models/pagination';
import { DialogService } from '@app/core/services/dialog.service';
import { selectCurrentWorkspaceIdentifier } from '@app/core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import {
  LucideArrowDown,
  LucideArrowUp,
  LucideArrowUpDown,
  LucideEllipsisVertical,
} from '@lucide/angular';
import { twMerge } from 'tailwind-merge';
import { IconButtonComponent } from '../button/icon-button.component';
import { CheckboxComponent } from '../checkbox/checkbox.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';
import { TablePaginationComponent } from '../table/table.component';
import { DatatableCellTemplateDirective } from './datatable-cell-template.directive';
import { classes } from './datatable-classes';
import { DatatableColumnsDialogComponent } from './datatable-columns-dialog.component';
import {
  loadColumnPreferences,
  reconcileColumnPreferences,
  saveColumnPreferences,
} from './datatable-columns.util';
import { DatatableEmptyDirective } from './datatable-empty.directive';
import {
  DatatableAccessor,
  DatatableCellContext,
  DatatableColumn,
  DatatableColumnPreference,
  DatatableDataSource,
  DatatableMenuItem,
  DatatableRowClass,
  DatatableSort,
  DatatableSortDirection,
} from './datatable.types';

@Component({
  selector: 'app-datatable',
  imports: [
    NgComponentOutlet,
    NgTemplateOutlet,
    CheckboxComponent,
    DropdownMenuComponent,
    IconButtonComponent,
    LucideArrowDown,
    LucideArrowUp,
    LucideArrowUpDown,
    LucideEllipsisVertical,
    MenuItemComponent,
    TablePaginationComponent,
  ],
  template: `
    <div [class]="mergedContainerClass()">
      <table [class]="mergedTableClass()">
        <ng-content select="caption" />
        <ng-content select="colgroup" />

        <thead
          [class]="mergedHeaderClass()"
          [class.sticky]="stickyHeader()"
          [class.top-0]="stickyHeader()"
          [class.z-10]="stickyHeader()">
          <tr>
            @if (showUtilityColumn()) {
              <th scope="col" class="w-10 p-1">
                @if (customizableColumns()) {
                  <button
                    class="h-5 rounded"
                    app-icon-button
                    type="button"
                    aria-label="Customize columns"
                    (click)="openColumnsDialog()">
                    <svg
                      lucideEllipsisVertical
                      class="text-foreground/50 h-4 w-4"></svg>
                  </button>
                } @else {
                  <span class="sr-only">Actions</span>
                }
              </th>
            }
            @if (selection()) {
              <th scope="col" class="w-10 px-2 py-3">
                <app-checkbox
                  [checked]="allSelected()"
                  (changed)="toggleAll($event)">
                  <span class="sr-only">Select all rows</span>
                </app-checkbox>
              </th>
            }
            @for (column of visibleColumns(); track column.id) {
              <th
                scope="col"
                [class]="resolvedHeaderCellClass(column)"
                [attr.aria-sort]="ariaSort(column)">
                @if (isSortable(column)) {
                  <button
                    class="hover:text-foreground focus:ring-foreground/30 inline-flex w-full items-center gap-1.5 rounded text-left transition-colors focus:ring-2 focus:outline-none"
                    type="button"
                    [attr.aria-label]="sortLabel(column)"
                    (click)="toggleSort(column)">
                    <span class="min-w-0 truncate">{{ column.header }}</span>
                    @switch (sortDirection(column)) {
                      @case ('asc') {
                        <svg lucideArrowUp class="h-3.5 w-3.5 shrink-0"></svg>
                      }
                      @case ('desc') {
                        <svg lucideArrowDown class="h-3.5 w-3.5 shrink-0"></svg>
                      }
                      @default {
                        <svg
                          lucideArrowUpDown
                          class="h-3.5 w-3.5 shrink-0 opacity-50"></svg>
                      }
                    }
                  </button>
                } @else {
                  {{ column.header }}
                }
              </th>
            }
          </tr>
        </thead>

        <tbody>
          @if (showSkeleton()) {
            @for (skeleton of skeletonRowRange(); track $index) {
              <tr [class]="classes.row">
                @if (showUtilityColumn()) {
                  <td class="px-2 align-middle">
                    <div
                      class="bg-foreground/10 h-4 w-4 animate-pulse rounded"></div>
                  </td>
                }
                @if (selection()) {
                  <td class="px-2 py-2 align-middle">
                    <div
                      class="bg-foreground/10 h-4 w-4 animate-pulse rounded"></div>
                  </td>
                }
                @for (column of visibleColumns(); track column.id) {
                  <td [class]="skeletonCellClass(column)">
                    <div
                      class="bg-foreground/10 h-4 animate-pulse rounded"
                      [style.width.%]="skeletonWidth($index)"></div>
                  </td>
                }
              </tr>
            }
          } @else {
            @for (row of visibleRows(); track trackRow($index, row)) {
              <tr [class]="resolvedRowClass(row, $index)">
                @if (showUtilityColumn()) {
                  <td class="px-2 align-middle">
                    @if (showMenuColumn()) {
                      <button
                        class="w-8"
                        app-icon-button
                        type="button"
                        aria-label="Row actions"
                        (click)="
                          dropdownmenu.toggle($any($event.currentTarget))
                        ">
                        <svg
                          lucideEllipsisVertical
                          class="text-foreground/30 h-4 w-4"></svg>
                      </button>

                      <app-dropdown-menu #dropdownmenu xPosition="after">
                        @for (menuItem of data().menu; track menuItem.label) {
                          <button
                            app-menu-item
                            type="button"
                            (click)="
                              selectMenuItem(menuItem, row, dropdownmenu)
                            ">
                            <ng-container
                              [ngComponentOutlet]="menuItem.icon"
                              [ngComponentOutletInputs]="iconInputs" />
                            <span>{{ menuItem.label }}</span>
                          </button>
                        }
                      </app-dropdown-menu>
                    }
                  </td>
                }
                @if (selection()) {
                  <td
                    class="px-2 py-2 align-middle"
                    (mousedown)="rangeSelectActive = $event.shiftKey">
                    <app-checkbox
                      [checked]="isSelected(row)"
                      (changed)="toggleRow(row, $event, $index)">
                      <span class="sr-only">Select row</span>
                    </app-checkbox>
                  </td>
                }
                @for (column of visibleColumns(); track column.id) {
                  <td [class]="resolvedCellClass(row, column, $index)">
                    @if (cellTemplate(column.id); as template) {
                      <ng-container
                        [ngTemplateOutlet]="template.templateRef"
                        [ngTemplateOutletContext]="
                          cellContext(row, column, $index)
                        " />
                    } @else {
                      {{ formattedCellValue(row, column) }}
                    }
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td
                  [class]="mergedEmptyCellClass()"
                  [attr.colspan]="emptyColumnSpan()">
                  <ng-content select="[appDatatableEmpty]" />
                  @if (!emptyState()) {
                    {{ emptyMessage() }}
                  }
                </td>
              </tr>
            }
          }
        </tbody>

        <ng-content select="tfoot" />
      </table>
    </div>

    <app-table-pagination
      itemLabel="tasks"
      [page]="currentPage()"
      [pageSize]="pageSize()"
      [pageSizeOptions]="[25, 50, 100]"
      [totalItems]="totalCount()"
      [totalPages]="totalPages()"
      (pageChange)="goToPage($event)"
      (pageSizeChange)="setPageSize($event)" />
  `,
})
export class DatatableComponent<T = unknown> implements OnDestroy {
  injector = inject(Injector);
  private dialog = inject(DialogService);
  private store = inject(Store);
  // The active workspace travels as a request header (set by the auth
  // interceptor), not in the URL or query params, so the resource's request is
  // identical across workspaces. Track the identifier so we can force a refetch
  // when it changes — otherwise the table keeps showing the previous
  // workspace's rows after a switch.
  private readonly workspaceIdentifier = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );
  private workspaceTracked = false;
  data = input.required<DatatableDataSource<T>>();
  selection = input(false, { transform: booleanAttribute });
  customizableColumns = input(false, { transform: booleanAttribute });
  containerClass = input('');
  tableClass = input('');
  headerClass = input('');
  rowClass = input<DatatableRowClass<T> | ''>('');
  emptyCellClass = input('');
  emptyMessage = input('No rows to display.');
  skeletonRows = input(8);
  stickyHeader = input(false);
  sort = model<DatatableSort | null>(null);
  selectionChanged = output<T[]>();
  // Emitted whenever the resource settles (initial load and every reload) so
  // hosts can react to row totals without reaching into the resource directly,
  // which would force it to evaluate before this component's inputs are bound.
  loaded = output<{ totalCount: number; hasValue: boolean }>();

  currentPage = signal(1);
  pageSize = signal(50);
  totalCount = computed(
    () => this.resourceRef.value()?.payload?.totalCount ?? 0
  );
  totalPages = computed(
    () => this.resourceRef.value()?.payload?.totalPages ?? 0
  );

  iconInputs = { size: 16 };
  classes = classes;
  lastResolvedRows = signal<readonly T[]>([]);
  cellTemplates = contentChildren<DatatableCellTemplateDirective<T>>(
    DatatableCellTemplateDirective
  );

  emptyState = contentChild<DatatableEmptyDirective>(DatatableEmptyDirective);

  mergedContainerClass = computed(() => {
    return twMerge(classes.container, this.containerClass());
  });

  mergedTableClass = computed(() => {
    return twMerge(classes.table, this.tableClass());
  });

  mergedHeaderClass = computed(() => {
    return twMerge(classes.header, this.headerClass());
  });

  mergedEmptyCellClass = computed(() => {
    return twMerge(classes.emptyCell, this.emptyCellClass());
  });

  cellTemplateMap = computed(() => {
    return new Map(
      this.cellTemplates().map((template) => [template.columnId(), template])
    );
  });

  resourceRef = httpResource<ClientResponse<Page<T>>>(() => ({
    url: this.data().resource.url,
    params: this.buildParams(),
  }));

  showMenuColumn = computed(() => Boolean(this.data().menu?.length));
  showUtilityColumn = computed(
    () => this.showMenuColumn() || this.customizableColumns()
  );
  columns = computed(() => this.data().columns);

  columnPreferences = signal<DatatableColumnPreference[] | null>(null);

  effectivePreferences = computed(() =>
    reconcileColumnPreferences(this.columns(), this.columnPreferences())
  );

  visibleColumns = computed(() => {
    const byId = new Map(this.columns().map((column) => [column.id, column]));

    return this.effectivePreferences()
      .filter((preference) => preference.visible)
      .map((preference) => byId.get(preference.id))
      .filter((column): column is DatatableColumn<T> => column != null);
  });

  buildParams = computed<Params>(() => {
    const sort = this.sort();
    const params = this.data().resource.params();

    const result: Record<string, string | number> = {
      pageSize: this.pageSize(),
      page: this.currentPage(),
      ...sort,
      ...params,
    };

    for (const key in result) {
      if (result[key] === undefined) {
        // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
        delete result[key];
      }
    }

    return result;
  });

  resourceLoading = computed(() => this.resourceRef?.isLoading() ?? false);

  // Show skeleton rows only when the resource is loading and there are no
  // previously resolved rows to keep on screen (i.e. the initial load).
  // During a reload we keep the stale rows visible via visibleRows() instead.
  showSkeleton = computed(
    () => this.resourceLoading() && this.lastResolvedRows().length === 0
  );

  skeletonRowRange = computed(() =>
    Array.from({ length: this.skeletonRows() })
  );

  currentRows = computed(() => {
    const resource = this.resourceRef;
    const dataSource = this.data();
    const response = resource?.hasValue() ? resource.value() : undefined;

    if (dataSource.rows) {
      return dataSource.rows(response);
    }

    return Array.isArray(response) ? (response as readonly T[]) : [];
  });

  visibleRows = computed(() => {
    const lastResolvedRows = this.lastResolvedRows();

    if (this.resourceLoading() && lastResolvedRows.length > 0) {
      return lastResolvedRows;
    }

    return this.currentRows();
  });

  selectionModel = signal(new Map<string | number, T>());
  selectedRows = computed(() => Array.from(this.selectionModel().values()));

  // Tracks whether the shift key was held at the start of the current click so
  // toggleRow can extend the selection as a range. Set on mousedown, which
  // always fires before the checkbox change event.
  rangeSelectActive = false;
  // Index of the last row toggled without shift; used as the range anchor.
  rangeAnchor: number | null = null;

  allSelected = computed(() => {
    const rows = this.visibleRows();

    if (rows.length === 0) return false;

    const model = this.selectionModel();

    return rows.every((row) => model.has(this.rowKey(row)));
  });

  constructor() {
    effect(() => {
      if (!this.customizableColumns()) return;

      const loaded = loadColumnPreferences(this.data().key);
      untracked(() => this.columnPreferences.set(loaded));
    });

    effect(() => {
      if (this.resourceLoading()) return;

      this.lastResolvedRows.set(this.currentRows());
    });

    effect(() => {
      if (this.resourceLoading()) return;

      const hasValue = this.resourceRef.hasValue();

      this.loaded.emit({
        totalCount: hasValue ? this.totalCount() : 0,
        hasValue,
      });
    });

    effect(() => {
      const reload = this.data().reloadSignal;

      if (!reload) {
        return;
      }

      reload();
      untracked(() => this.resourceRef.reload());
    });

    effect(() => {
      // Refetch when the active workspace changes. Skip the first observation
      // so we don't double-fetch the initial load (the resource fetches itself
      // on creation).
      this.workspaceIdentifier();

      untracked(() => {
        if (!this.workspaceTracked) {
          this.workspaceTracked = true;
          return;
        }

        this.resourceRef.reload();
      });
    });
  }

  ngOnDestroy() {
    this.resourceRef?.destroy();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
  }

  setPageSize(pageSize: number) {
    this.pageSize.set(pageSize);
    this.currentPage.set(1);
  }

  toggleSort(column: DatatableColumn<T>) {
    if (!this.isSortable(column)) return;

    const sort = this.sort();

    if (sort?.sortBy !== column.id) {
      this.sort.set({ sortBy: column.id, sortDirection: 'asc' });
      return;
    }

    if (sort.sortDirection === 'asc') {
      this.sort.set({ sortBy: column.id, sortDirection: 'desc' });
      return;
    }

    this.sort.set(null);
  }

  isSelected(row: T): boolean {
    return this.selectionModel().has(this.rowKey(row));
  }

  toggleRow(row: T, selected: boolean, index: number) {
    const next = new Map(this.selectionModel());
    const rangeSelect = this.rangeSelectActive;
    this.rangeSelectActive = false;

    if (rangeSelect && this.rangeAnchor !== null) {
      const rows = this.visibleRows();
      const start = Math.min(this.rangeAnchor, index);
      const end = Math.max(this.rangeAnchor, index);

      for (let i = start; i <= end; i++) {
        const rangeRow = rows[i];
        const key = this.rowKey(rangeRow);

        if (selected) {
          next.set(key, rangeRow);
        } else {
          next.delete(key);
        }
      }

      this.commitSelection(next);
      return;
    }

    const key = this.rowKey(row);

    if (selected) {
      next.set(key, row);
    } else {
      next.delete(key);
    }

    this.rangeAnchor = index;
    this.commitSelection(next);
  }

  toggleAll(selected: boolean) {
    const next = new Map(this.selectionModel());

    for (const row of this.visibleRows()) {
      const key = this.rowKey(row);

      if (selected) {
        next.set(key, row);
      } else {
        next.delete(key);
      }
    }

    this.commitSelection(next);
  }

  rowKey(row: T): string | number {
    return this.data().trackBy(0, row);
  }

  commitSelection(next: Map<string | number, T>) {
    this.selectionModel.set(next);
    this.selectionChanged.emit(Array.from(next.values()));
  }

  clearSelection() {
    this.rangeAnchor = null;

    if (this.selectionModel().size === 0) return;

    this.commitSelection(new Map());
  }

  openColumnsDialog() {
    const byId = new Map(this.columns().map((column) => [column.id, column]));
    const items = this.effectivePreferences().map((preference) => ({
      id: preference.id,
      header: byId.get(preference.id)?.header ?? preference.id,
      visible: preference.visible,
    }));

    const ref = this.dialog.open<DatatableColumnPreference[]>(
      DatatableColumnsDialogComponent,
      { data: { items } }
    );

    ref.closed.subscribe((result) => {
      if (!result) return;

      this.columnPreferences.set(result);
      saveColumnPreferences(this.data().key, result);
    });
  }

  isSortable(column: DatatableColumn<T>): boolean {
    return Boolean(column.sortable || column.sortKey);
  }

  sortDirection(column: DatatableColumn<T>): DatatableSortDirection | null {
    const sort = this.sort();

    return sort?.sortBy === column.id ? sort.sortDirection : null;
  }

  ariaSort(column: DatatableColumn<T>) {
    const direction = this.sortDirection(column);

    if (!direction) return null;

    return direction === 'asc' ? 'ascending' : 'descending';
  }

  sortLabel(column: DatatableColumn<T>): string {
    const label = column.ariaLabel ?? column.header;
    const direction = this.sortDirection(column);

    if (direction === 'asc') {
      return `Sort ${label} descending`;
    }

    if (direction === 'desc') {
      return `Clear ${label} sort`;
    }

    return `Sort ${label} ascending`;
  }

  cellTemplate(columnId: string) {
    return this.cellTemplateMap().get(columnId) ?? null;
  }

  selectMenuItem(
    menuItem: DatatableMenuItem<T>,
    row: T,
    menu: DropdownMenuComponent
  ) {
    menu.close();
    menuItem.onClick(row);
  }

  cellContext(
    row: T,
    column: DatatableColumn<T>,
    rowIndex: number
  ): DatatableCellContext<T> {
    return {
      $implicit: row,
      row,
      value: this.cellValue(row, column),
      column,
      rowIndex,
    };
  }

  cellValue(row: T, column: DatatableColumn<T>): unknown {
    return readValue(row, column.accessor ?? column.id);
  }

  formattedCellValue(row: T, column: DatatableColumn<T>): unknown {
    const value = this.cellValue(row, column);

    return column.format ? column.format(value, row, column) : value;
  }

  trackRow(index: number, row: T) {
    return this.data().trackBy(index, row);
  }

  emptyColumnSpan(): number {
    return Math.max(
      1,
      this.visibleColumns().length +
        (this.showUtilityColumn() ? 1 : 0) +
        (this.selection() ? 1 : 0)
    );
  }

  skeletonCellClass(column: DatatableColumn<T>): string {
    return twMerge(
      classes.cell,
      alignmentClass(column.align),
      column.widthClass
    );
  }

  // Vary the placeholder bar widths so the skeleton reads as content rather
  // than a uniform grid. Deterministic so it stays stable across renders.
  skeletonWidth(index: number): number {
    const widths = [80, 55, 70, 45, 90, 60];

    return widths[index % widths.length];
  }

  resolvedHeaderCellClass(column: DatatableColumn<T>): string {
    return twMerge(
      classes.headerCell,
      alignmentClass(column.align),
      column.widthClass,
      column.headerClass
    );
  }

  resolvedRowClass(row: T, rowIndex: number): string {
    const rowClass = this.rowClass();

    return twMerge(
      classes.row,
      typeof rowClass === 'function' ? rowClass(row, rowIndex) : rowClass
    );
  }

  resolvedCellClass(
    row: T,
    column: DatatableColumn<T>,
    rowIndex: number
  ): string {
    const cellClass =
      typeof column.cellClass === 'function'
        ? column.cellClass(row, column, rowIndex)
        : column.cellClass;

    return twMerge(
      classes.cell,
      alignmentClass(column.align),
      column.widthClass,
      cellClass
    );
  }
}

function readValue<T>(
  row: T,
  accessor: DatatableAccessor<T> | string
): unknown {
  if (typeof accessor === 'function') {
    return accessor(row);
  }

  return (row as Record<string, unknown>)[accessor as string];
}

function alignmentClass(align: DatatableColumn['align']): string {
  switch (align) {
    case 'center':
      return 'text-center';
    case 'end':
      return 'text-right';
    default:
      return '';
  }
}
