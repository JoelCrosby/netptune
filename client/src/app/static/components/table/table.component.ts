import {
  Component,
  Directive,
  computed,
  input,
  model,
  output,
} from '@angular/core';
import {
  LucideCheck,
  LucideChevronLeft,
  LucideChevronRight,
  LucideChevronsLeft,
  LucideChevronsRight,
  LucideChevronDown,
} from '@lucide/angular';
import { twMerge } from 'tailwind-merge';
import { IconButtonComponent } from '../button/icon-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';

const defaultContainerClass =
  'bg-background border-border rounded border custom-scroll';
const defaultTableClass = 'w-full text-sm custom-scroll';

@Component({
  selector: 'app-table',
  template: `
    <div [class]="mergedContainerClass()">
      <table [class]="mergedTableClass()">
        <ng-content select="caption" />
        <ng-content select="colgroup" />
        <ng-content select="thead" />
        <ng-content select="tbody" />
        <ng-content select="tfoot" />
      </table>
    </div>

    <ng-content select="app-table-pagination" />
  `,
})
export class TableComponent {
  readonly containerClass = input('');
  readonly tableClass = input('');

  protected readonly mergedContainerClass = computed(() =>
    twMerge(defaultContainerClass, this.containerClass())
  );
  protected readonly mergedTableClass = computed(() =>
    twMerge(defaultTableClass, this.tableClass())
  );
}

@Component({
  selector: 'app-table-pagination',
  imports: [
    IconButtonComponent,
    LucideChevronLeft,
    LucideChevronRight,
    LucideChevronsLeft,
    LucideChevronsRight,
    LucideChevronDown,
    LucideCheck,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    <div
      class="flex flex-col gap-3 px-2 py-4 text-sm sm:flex-row sm:items-center sm:justify-between">
      @if (selectedItems() !== null) {
        <div class="text-muted">
          {{ selectedItems() }} of {{ totalItems() }}
          {{ itemLabel() }} selected.
        </div>
      } @else {
        <div class="hidden sm:block"></div>
      }

      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-6">
        <div class="flex items-center gap-2">
          <span class="text-foreground font-medium whitespace-nowrap">
            Rows per page
          </span>
          <button
            #pageSizeTrigger
            class="border-border bg-background text-foreground focus:ring-foreground/30 hover:bg-foreground/5 inline-flex h-8 min-w-18 items-center justify-between gap-2 rounded border px-2 text-sm font-medium transition-colors focus:ring-2 focus:outline-none disabled:pointer-events-none disabled:opacity-50"
            type="button"
            aria-haspopup="menu"
            aria-label="Rows per page"
            (click)="pageSizeMenu.toggle(pageSizeTrigger)">
            <span>{{ pageSize() }}</span>
            <svg lucideChevronDown class="h-4 w-4 opacity-70"></svg>
          </button>

          <app-dropdown-menu #pageSizeMenu xPosition="before">
            @for (size of normalizedPageSizeOptions(); track size) {
              <button
                app-menu-item
                type="button"
                role="menuitemradio"
                [attr.aria-checked]="size === pageSize()"
                [class.bg-neutral-100]="size === pageSize()"
                [class.dark:bg-neutral-800]="size === pageSize()"
                (click)="selectPageSize(size, pageSizeMenu)">
                <span class="flex h-4 w-4 shrink-0 items-center justify-center">
                  @if (size === pageSize()) {
                    <svg lucideCheck class="h-4 w-4"></svg>
                  }
                </span>
                <span>{{ size }}</span>
              </button>
            }
          </app-dropdown-menu>
        </div>

        <div class="text-foreground flex items-center justify-between gap-4">
          <span class="font-medium whitespace-nowrap">
            Page {{ page() }} of {{ resolvedTotalPages() }}
          </span>

          <div class="flex items-center gap-1">
            @if (showBoundaryButtons()) {
              <button
                app-icon-button
                class="border-border hidden h-8 w-8 rounded border sm:inline-flex"
                type="button"
                aria-label="Go to first page"
                [disabled]="isFirstPage()"
                (click)="goToPage(1)">
                <svg lucideChevronsLeft class="h-4 w-4"></svg>
              </button>
            }
            <button
              app-icon-button
              class="border-border h-8 w-8 rounded border"
              type="button"
              aria-label="Go to previous page"
              [disabled]="isFirstPage()"
              (click)="goToPage(page() - 1)">
              <svg lucideChevronLeft class="h-4 w-4"></svg>
            </button>
            <button
              app-icon-button
              class="border-border h-8 w-8 rounded border"
              type="button"
              aria-label="Go to next page"
              [disabled]="isLastPage()"
              (click)="goToPage(page() + 1)">
              <svg lucideChevronRight class="h-4 w-4"></svg>
            </button>
            @if (showBoundaryButtons()) {
              <button
                app-icon-button
                class="border-border hidden h-8 w-8 rounded border sm:inline-flex"
                type="button"
                aria-label="Go to last page"
                [disabled]="isLastPage()"
                (click)="goToPage(resolvedTotalPages())">
                <svg lucideChevronsRight class="h-4 w-4"></svg>
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class TablePaginationComponent {
  readonly page = input(1);
  readonly pageSize = model(10);
  readonly pageSizeOptions = input<number[]>([10, 20, 30, 40, 50]);
  readonly totalItems = input(0);
  readonly totalPages = input<number | null>(null);
  readonly selectedItems = input<number | null>(null);
  readonly itemLabel = input('rows');
  readonly showBoundaryButtons = input(true);

  readonly pageChange = output<number>();

  protected readonly normalizedPageSizeOptions = computed(() =>
    [...new Set([...this.pageSizeOptions(), this.pageSize()])].sort(
      (left, right) => left - right
    )
  );

  protected readonly resolvedTotalPages = computed(() => {
    const totalPages = this.totalPages();

    if (totalPages !== null) {
      return Math.max(1, totalPages);
    }

    return Math.max(1, Math.ceil(this.totalItems() / this.safePageSize()));
  });

  protected readonly isFirstPage = computed(() => this.page() <= 1);
  protected readonly isLastPage = computed(
    () => this.page() >= this.resolvedTotalPages()
  );

  protected selectPageSize(pageSize: number, menu: DropdownMenuComponent) {
    menu.close();

    if (pageSize !== this.pageSize()) {
      this.pageSize.set(pageSize);
    }
  }

  protected goToPage(page: number) {
    const nextPage = Math.min(Math.max(1, page), this.resolvedTotalPages());

    if (nextPage === this.page()) return;

    this.pageChange.emit(nextPage);
  }

  private safePageSize(): number {
    return Math.max(1, this.pageSize());
  }
}

@Directive({
  selector: 'thead[appTableHead]',
  host: {
    class: 'border-border border-b',
    '[class.sticky]': 'sticky',
    '[class.top-0]': 'sticky',
    '[class.z-10]': 'sticky',
  },
})
export class TableHeadDirective {
  readonly sticky = input(false);
}

@Directive({
  selector: 'tr[appTableHeaderRow]',
  host: {
    class: 'text-left text-xs font-medium tracking-wide',
  },
})
export class TableHeaderRowDirective {}

@Directive({
  selector: 'tr[appTableRow]',
  host: {
    class:
      'border-border hover:bg-foreground/5 border-b last:border-0 transition-colors',
  },
})
export class TableRowDirective {}

@Directive({
  selector: 'td[appTableEmptyCell]',
  host: {
    class: 'text-muted px-4 py-10 text-center',
  },
})
export class TableEmptyCellDirective {}
