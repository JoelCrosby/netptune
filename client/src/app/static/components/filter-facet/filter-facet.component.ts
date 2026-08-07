import { Component, computed, input, output } from '@angular/core';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';

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

@Component({
  selector: 'app-filter-facet',
  imports: [BadgeComponent, CheckboxComponent],
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

      @if (options().length === 0) {
        <p class="text-muted px-4 py-10 text-center text-sm">
          {{ emptyMessage() }}
        </p>
      } @else {
        <div class="custom-scroll p-2" [class]="listClass()">
          @for (option of options(); track option.value) {
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

  readonly toggled = output<FilterFacetToggle>();
  readonly cleared = output();

  protected readonly selectedCount = computed(() => this.selected().length);

  protected readonly listClass = computed(() => {
    const columns = this.columns();
    const grid = columns > 1 ? `grid gap-x-2 ${columnClasses[columns]}` : '';

    return `${grid} ${heightClasses[this.maxHeight()]}`.trim();
  });

  private readonly selectedValues = computed(() => new Set(this.selected()));

  protected isSelected(value: string): boolean {
    return this.selectedValues().has(value);
  }
}
