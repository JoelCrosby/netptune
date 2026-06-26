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
import { DatatableEmptyDirective } from './datatable-empty.directive';
import {
  DatatableAccessor,
  DatatableCellContext,
  DatatableColumn,
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
            @if (showMenuColumn()) {
              <th scope="col" class="w-10 px-2 py-3">
                <span class="sr-only">Actions</span>
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
            @for (column of columns(); track column.id) {
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
          @for (row of visibleRows(); track trackRow($index, row)) {
            <tr [class]="resolvedRowClass(row, $index)">
              @if (showMenuColumn()) {
                <td class="px-2 align-middle">
                  <button
                    class="w-8"
                    app-icon-button
                    type="button"
                    aria-label="Row actions"
                    (click)="dropdownmenu.toggle($any($event.currentTarget))">
                    <svg
                      lucideEllipsisVertical
                      class="text-foreground/30 h-4 w-4"></svg>
                  </button>

                  <app-dropdown-menu #dropdownmenu xPosition="after">
                    @for (menuItem of data().menu; track menuItem.label) {
                      <button
                        app-menu-item
                        type="button"
                        (click)="selectMenuItem(menuItem, row, dropdownmenu)">
                        <ng-container
                          [ngComponentOutlet]="menuItem.icon"
                          [ngComponentOutletInputs]="iconInputs" />
                        <span>{{ menuItem.label }}</span>
                      </button>
                    }
                  </app-dropdown-menu>
                </td>
              }
              @if (selection()) {
                <td class="px-2 py-2 align-middle">
                  <app-checkbox
                    [checked]="isSelected(row)"
                    (changed)="toggleRow(row, $event)">
                    <span class="sr-only">Select row</span>
                  </app-checkbox>
                </td>
              }
              @for (column of columns(); track column.id) {
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
  data = input.required<DatatableDataSource<T>>();
  selection = input(false, { transform: booleanAttribute });
  containerClass = input('');
  tableClass = input('');
  headerClass = input('');
  rowClass = input<DatatableRowClass<T> | ''>('');
  emptyCellClass = input('');
  emptyMessage = input('No rows to display.');
  stickyHeader = input(false);
  sort = model<DatatableSort | null>(null);
  selectionChanged = output<T[]>();

  currentPage = signal(1);
  pageSize = signal(50);
  totalCount = computed(
    () => this.resourceRef.value()?.payload?.totalCount ?? 0
  );
  totalPages = computed(
    () => this.resourceRef.value()?.payload?.totalPages ?? 0
  );

  iconInputs = { size: 16 };
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

  showMenuColumn = computed(() => this.data().menu?.length);
  columns = computed(() => this.data().columns);

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

  allSelected = computed(() => {
    const rows = this.visibleRows();

    if (rows.length === 0) return false;

    const model = this.selectionModel();

    return rows.every((row) => model.has(this.rowKey(row)));
  });

  constructor() {
    effect(() => {
      if (this.resourceLoading()) return;

      this.lastResolvedRows.set(this.currentRows());
    });

    effect(() => {
      const reload = this.data().reloadSignal;

      if (!reload) {
        return;
      }

      reload();
      untracked(() => this.resourceRef.reload());
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

  toggleRow(row: T, selected: boolean) {
    const next = new Map(this.selectionModel());
    const key = this.rowKey(row);

    if (selected) {
      next.set(key, row);
    } else {
      next.delete(key);
    }

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
    if (this.selectionModel().size === 0) return;

    this.commitSelection(new Map());
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
      this.columns().length +
        (this.showMenuColumn() ? 1 : 0) +
        (this.selection() ? 1 : 0)
    );
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
