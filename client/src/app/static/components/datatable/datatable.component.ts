import { NgComponentOutlet, NgTemplateOutlet } from '@angular/common';
import { HttpResourceRef } from '@angular/common/http';
import {
  Component,
  Injector,
  OnDestroy,
  OnInit,
  booleanAttribute,
  computed,
  contentChild,
  contentChildren,
  effect,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
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
import { DatatableCellTemplateDirective } from './datatable-cell-template.directive';
import { DatatableEmptyDirective } from './datatable-empty.directive';
import {
  DatatableAccessor,
  DatatableCellContext,
  DatatableColumn,
  DatatableDataSource,
  DatatableLoadParams,
  DatatableMenuItem,
  DatatableRowClass,
  DatatableSort,
  DatatableSortDirection,
} from './datatable.types';
import { classes } from './datatable-classes';

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
                <span class="sr-only">Select row</span>
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
                  <app-checkbox />
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

    <ng-content select="app-table-pagination" />
  `,
})
export class DatatableComponent<T = unknown, R = unknown>
  implements OnInit, OnDestroy
{
  private readonly injector = inject(Injector);

  readonly data = input.required<DatatableDataSource<T, R>>();
  readonly selection = input(false, { transform: booleanAttribute });
  readonly containerClass = input('');
  readonly tableClass = input('');
  readonly headerClass = input('');
  readonly rowClass = input<DatatableRowClass<T> | ''>('');
  readonly emptyCellClass = input('');
  readonly emptyMessage = input('No rows to display.');
  readonly stickyHeader = input(false);
  readonly sort = model<DatatableSort | null>(null);

  protected readonly iconInputs = { size: 16 };
  private readonly resourceRef = signal<HttpResourceRef<R> | null>(null);
  private readonly lastResolvedRows = signal<readonly T[]>([]);
  private readonly cellTemplates = contentChildren<
    DatatableCellTemplateDirective<T>
  >(DatatableCellTemplateDirective);
  protected readonly emptyState = contentChild<DatatableEmptyDirective>(
    DatatableEmptyDirective
  );

  protected readonly mergedContainerClass = computed(() =>
    twMerge(classes.defaultContainerClass, this.containerClass())
  );
  protected readonly mergedTableClass = computed(() =>
    twMerge(classes.defaultTableClass, this.tableClass())
  );
  protected readonly mergedHeaderClass = computed(() =>
    twMerge(classes.defaultHeaderClass, this.headerClass())
  );
  protected readonly mergedEmptyCellClass = computed(() =>
    twMerge(classes.defaultEmptyCellClass, this.emptyCellClass())
  );
  protected readonly cellTemplateMap = computed(
    () =>
      new Map(
        this.cellTemplates().map((template) => [template.columnId(), template])
      )
  );
  protected readonly showMenuColumn = computed(() => this.data().menu?.length);
  protected readonly columns = computed(() => this.data().columns);
  private readonly loadParams = computed<DatatableLoadParams>(() => {
    const sort = this.sort();

    if (!sort) return { sort: null };

    const column = this.columns().find(({ id }) => id === sort.columnId);

    if (!column || !this.isSortable(column)) return { sort: null };

    return {
      sort: {
        ...sort,
        field: column.sortKey ?? column.id,
      },
    };
  });

  private readonly resourceLoading = computed(
    () => this.resourceRef()?.isLoading() ?? false
  );

  private readonly currentRows = computed(() => {
    const resource = this.resourceRef();
    const dataSource = this.data();
    const response = resource?.hasValue() ? resource.value() : undefined;

    if (dataSource.rows) {
      return dataSource.rows(response);
    }

    return Array.isArray(response) ? (response as readonly T[]) : [];
  });

  protected readonly visibleRows = computed(() => {
    const lastResolvedRows = this.lastResolvedRows();

    if (this.resourceLoading() && lastResolvedRows.length > 0) {
      return lastResolvedRows;
    }

    return this.currentRows();
  });

  constructor() {
    effect(() => {
      if (this.resourceLoading()) return;

      this.lastResolvedRows.set(this.currentRows());
    });
  }

  ngOnInit() {
    this.resourceRef.set(this.data().resource(this.loadParams, this.injector));
  }

  ngOnDestroy() {
    this.resourceRef()?.destroy();
  }

  protected toggleSort(column: DatatableColumn<T>) {
    if (!this.isSortable(column)) return;

    const sort = this.sort();

    if (sort?.columnId !== column.id) {
      this.sort.set({ columnId: column.id, direction: 'asc' });
      return;
    }

    if (sort.direction === 'asc') {
      this.sort.set({ columnId: column.id, direction: 'desc' });
      return;
    }

    this.sort.set(null);
  }

  protected isSortable(column: DatatableColumn<T>): boolean {
    return Boolean(column.sortable || column.sortKey);
  }

  protected sortDirection(
    column: DatatableColumn<T>
  ): DatatableSortDirection | null {
    const sort = this.sort();

    return sort?.columnId === column.id ? sort.direction : null;
  }

  protected ariaSort(column: DatatableColumn<T>) {
    const direction = this.sortDirection(column);

    if (!direction) return null;

    return direction === 'asc' ? 'ascending' : 'descending';
  }

  protected sortLabel(column: DatatableColumn<T>): string {
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

  protected cellTemplate(columnId: string) {
    return this.cellTemplateMap().get(columnId) ?? null;
  }

  protected selectMenuItem(
    menuItem: DatatableMenuItem<T>,
    row: T,
    menu: DropdownMenuComponent
  ) {
    menu.close();
    menuItem.onClick(row);
  }

  protected cellContext(
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

  protected cellValue(row: T, column: DatatableColumn<T>): unknown {
    return readValue(row, column.accessor ?? column.id);
  }

  protected formattedCellValue(row: T, column: DatatableColumn<T>): unknown {
    const value = this.cellValue(row, column);

    return column.format ? column.format(value, row, column) : value;
  }

  protected trackRow(index: number, row: T) {
    return this.data().trackBy(index, row);
  }

  protected emptyColumnSpan(): number {
    return Math.max(
      1,
      this.columns().length +
        (this.showMenuColumn() ? 1 : 0) +
        (this.selection() ? 1 : 0)
    );
  }

  protected resolvedHeaderCellClass(column: DatatableColumn<T>): string {
    return twMerge(
      classes.defaultHeaderCellClass,
      alignmentClass(column.align),
      column.widthClass,
      column.headerClass
    );
  }

  protected resolvedRowClass(row: T, rowIndex: number): string {
    const rowClass = this.rowClass();

    return twMerge(
      classes.defaultRowClass,
      typeof rowClass === 'function' ? rowClass(row, rowIndex) : rowClass
    );
  }

  protected resolvedCellClass(
    row: T,
    column: DatatableColumn<T>,
    rowIndex: number
  ): string {
    const cellClass =
      typeof column.cellClass === 'function'
        ? column.cellClass(row, column, rowIndex)
        : column.cellClass;

    return twMerge(
      classes.defaultCellClass,
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
