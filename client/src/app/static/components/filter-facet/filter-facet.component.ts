import {
  Component,
  computed,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';

export interface FilterFacetOption {
  value: string;
  label: string;
}

export interface FilterFacetToggle {
  value: string;
  selected: boolean;
}

export type FilterFacetColumns = 1 | 2 | 3;
export type FilterFacetHeight = 'sm' | 'md' | 'lg' | 'none';

const columnClasses: Record<FilterFacetColumns, string> = {
  1: '',
  2: 'sm:grid-cols-2',
  3: 'sm:grid-cols-2 lg:grid-cols-3',
};

const heightClasses: Record<FilterFacetHeight, string> = {
  sm: 'max-h-40 overflow-y-auto',
  md: 'max-h-52 overflow-y-auto',
  lg: 'max-h-80 overflow-y-auto',
  none: '',
};

const nearBottomThresholdPx = 48;

@Component({
  selector: 'app-filter-facet',
  imports: [BadgeComponent, CheckboxComponent, FilterInputComponent],
  host: { class: 'block' },
  template: `
    <div class="border-border bg-background flex flex-col rounded-lg border">
      <div
        class="border-border flex items-center justify-between gap-3 border-b px-4 py-3">
        <div class="flex min-w-0 items-center gap-2">
          <span class="truncate text-sm font-medium">{{ label() }}</span>

          @if (selectedCount() > 0) {
            <app-badge color="primary" shape="rounded">
              {{ selectedCount() }}
            </app-badge>
          }
        </div>

        <div class="flex shrink-0 items-center gap-3">
          <ng-content select="[facetActions]" />

          @if (selectedCount() > 0) {
            <button
              type="button"
              class="text-muted hover:text-foreground text-xs transition-colors"
              (click)="cleared.emit()">
              <span i18n="Button that clears one group of filters">Clear</span>
            </button>
          }
        </div>
      </div>

      @if (showSearch()) {
        <app-filter-input
          appearance="bare"
          [value]="query()"
          [placeholder]="searchPlaceholder()"
          [ariaLabel]="searchAriaLabel()"
          (valueChange)="query.set($event)" />
      }

      @if (options().length === 0) {
        <p class="text-muted px-4 py-10 text-center text-sm">
          {{ emptyMessage() }}
        </p>
      } @else if (renderedOptions().length === 0) {
        <p class="text-muted px-4 py-10 text-center text-sm">
          {{ noResultsMessage() }}
        </p>
      } @else {
        <div
          class="custom-scroll p-2"
          [class]="listClass()"
          (scroll)="onScroll($event)">
          @for (option of renderedOptions(); track option.value) {
            <div
              class="hover:bg-foreground/5 rounded-md px-2 py-2.5 transition-colors">
              <app-checkbox
                class="block"
                [checked]="isSelected(option.value)"
                (changed)="
                  toggled.emit({ value: option.value, selected: $event })
                ">
                {{ option.label }}
              </app-checkbox>
            </div>
          }
        </div>

        @if (hiddenCount(); as hidden) {
          <button
            type="button"
            class="text-muted hover:text-foreground border-border w-full cursor-pointer border-t px-4 py-2 text-center text-xs transition-colors"
            (click)="loadMore()">
            {{ moreLabel(hidden) }}
          </button>
        }
      }
    </div>
  `,
})
export class FilterFacetComponent {
  readonly label = input.required<string>();
  readonly options = input.required<FilterFacetOption[]>();
  readonly selected = input<string[]>([]);
  readonly emptyMessage = input('');
  readonly columns = input<FilterFacetColumns>(1);
  readonly maxHeight = input<FilterFacetHeight>('md');
  readonly pageSize = input(50);
  /** Below this many options the list is short enough to scan without a search box. */
  readonly searchThreshold = input(10);
  readonly searchPlaceholder = input(
    $localize`:Placeholder in the box that narrows a filter's options:Search`
  );
  readonly noResultsMessage = input(
    $localize`:Shown when a filter's search term matches no options:No matches`
  );

  readonly toggled = output<FilterFacetToggle>();
  readonly cleared = output();

  protected readonly query = signal('');

  protected readonly selectedCount = computed(() => this.selected().length);

  protected readonly showSearch = computed(() => {
    return this.options().length > this.searchThreshold();
  });

  protected readonly searchAriaLabel = computed(() => {
    return $localize`:Accessible name of the box that narrows one filter group's options. GROUP is the group's heading:Search ${this.label()}:GROUP:`;
  });

  private readonly selectedValues = computed(() => new Set(this.selected()));

  protected readonly filteredOptions = computed(() => {
    const query = this.query().trim().toLowerCase();
    const options = this.options();

    if (!query) {
      return options;
    }

    return options.filter((option) => this.survivesQuery(option, query));
  });

  private readonly page = linkedSignal<string, number>({
    source: () => this.query(),
    computation: () => 1,
  });

  protected readonly renderedOptions = computed(() => {
    return this.filteredOptions().slice(0, this.page() * this.pageSize());
  });

  protected readonly hiddenCount = computed(() => {
    return this.filteredOptions().length - this.renderedOptions().length;
  });

  protected readonly listClass = computed(() => {
    const columns = this.columns();
    const grid = columns > 1 ? `grid gap-x-2 ${columnClasses[columns]}` : '';

    return `${grid} ${heightClasses[this.maxHeight()]}`.trim();
  });

  protected isSelected(value: string): boolean {
    return this.selectedValues().has(value);
  }

  protected moreLabel(hidden: number): string {
    return $localize`:Button that renders the next page of a filter's options. COUNT is how many options remain:Show more (${hidden}:COUNT: remaining)`;
  }

  protected loadMore() {
    this.page.update((page) => page + 1);
  }

  protected onScroll(event: Event) {
    if (this.hiddenCount() <= 0) return;

    const element = event.target as HTMLElement;
    const distanceToBottom =
      element.scrollHeight - element.scrollTop - element.clientHeight;

    if (distanceToBottom > nearBottomThresholdPx) return;

    this.loadMore();
  }

  private survivesQuery(option: FilterFacetOption, query: string): boolean {
    const matches = option.label.toLowerCase().includes(query);

    return matches || this.selectedValues().has(option.value);
  }
}
