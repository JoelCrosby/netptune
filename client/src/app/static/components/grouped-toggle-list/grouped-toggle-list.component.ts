import {
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { toggleInSet } from '@core/util/signals';
import {
  LucideChevronRight,
  LucideChevronsUpDown,
  LucideDynamicIcon,
  LucideIconInput,
  LucideSearch,
} from '@lucide/angular';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { FilterInputComponent } from '../filter-input/filter-input.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '../segmented-control/segmented-control.component';
import { SwitchComponent } from '../switch/switch.component';
import { InlineButtonComponent } from '../button/inline-button.component';
import { StrokedButtonComponent } from '../button/stroked-button.component';

export interface ToggleListRow {
  key: string;
  label: string;
  checked: boolean;
  ariaLabel: string;
  hint?: string;
  isHintHighlighted?: boolean;
  icon?: LucideIconInput;
  isDimmed?: boolean;
  isDisabled?: boolean;
  isResettable?: boolean;
}

export interface ToggleListGroup {
  key: string;
  heading: string;
  summary: string;
  actionLabel?: string;
  rows: ToggleListRow[];
}

export interface ToggleListChange {
  key: string;
  checked: boolean;
}

@Component({
  selector: 'app-grouped-toggle-list',
  imports: [
    EmptyStateComponent,
    FilterInputComponent,
    InlineButtonComponent,
    LucideChevronRight,
    LucideChevronsUpDown,
    LucideDynamicIcon,
    LucideSearch,
    SegmentedControlComponent,
    StrokedButtonComponent,
    SwitchComponent,
  ],
  host: { class: 'block w-full' },
  template: `
    <div
      class="border-border bg-card z-10 flex flex-wrap items-center gap-2.5 border-b px-6 py-3 max-md:sticky max-md:top-0">
      <app-filter-input
        class="min-w-48 flex-1"
        [(value)]="query"
        [placeholder]="searchPlaceholder()" />

      <app-segmented-control
        class="shrink-0"
        [options]="filterOptions()"
        [(value)]="filter"
        [ariaLabel]="filterAriaLabel()" />

      <button
        app-stroked-button
        type="button"
        color="neutral"
        size="small"
        class="shrink-0"
        [disabled]="filtering()"
        (click)="toggleAllGroups()">
        <svg lucideChevronsUpDown class="h-3.5 w-3.5" aria-hidden="true"></svg>
        @if (isAnyGroupOpen()) {
          <span i18n="Button that collapses every group in a toggle list">
            Collapse all
          </span>
        } @else {
          <span i18n="Button that expands every group in a toggle list">
            Expand all
          </span>
        }
      </button>
    </div>

    <div class="md:max-h-[min(40rem,60dvh)] md:overflow-y-auto">
      @for (group of groups(); track group.key) {
        @let isOpen = filtering() || expanded().has(group.key);

        <div class="border-border border-b last:border-b-0">
          <div class="flex items-center gap-3 pr-6 pl-4">
            <button
              type="button"
              class="text-foreground focus-visible:ring-primary flex min-w-0 flex-1 cursor-pointer items-center gap-3 rounded-md px-2 py-3 text-left outline-none focus-visible:ring-2"
              [attr.aria-expanded]="isOpen"
              [disabled]="filtering()"
              (click)="toggleGroup(group.key)">
              <svg
                lucideChevronRight
                aria-hidden="true"
                class="text-muted h-3.5 w-3.5 shrink-0 transition-transform duration-150"
                [class.rotate-90]="isOpen"></svg>
              <span class="text-sm font-semibold">{{ group.heading }}</span>
              <span class="text-muted text-xs">{{ group.summary }}</span>
            </button>

            @if (group.actionLabel) {
              <button
                type="button"
                app-inline-button
                color="muted"
                appearance="soft"
                class="border-border shrink-0 rounded-full border font-semibold"
                (click)="groupAction.emit(group.key)">
                {{ group.actionLabel }}
              </button>
            }
          </div>

          @if (isOpen) {
            <div class="flex flex-col px-6 pb-2.5">
              @for (row of group.rows; track row.key) {
                <div
                  class="hover:bg-hover flex items-center gap-3 rounded-lg py-2 pr-2 pl-8.5">
                  @if (row.icon) {
                    <svg
                      [lucideIcon]="row.icon"
                      aria-hidden="true"
                      class="h-4 w-4 shrink-0"
                      [class.opacity-40]="row.isDimmed"></svg>
                  }

                  <div class="min-w-0 flex-1" [class.opacity-40]="row.isDimmed">
                    <p class="truncate text-sm font-medium">{{ row.label }}</p>
                    @if (row.hint) {
                      <p
                        class="text-xs"
                        [class.text-primary]="row.isHintHighlighted"
                        [class.text-muted]="!row.isHintHighlighted">
                        {{ row.hint }}
                      </p>
                    }
                  </div>

                  @if (row.isResettable) {
                    <button
                      type="button"
                      app-inline-button
                      color="muted"
                      appearance="soft"
                      class="shrink-0"
                      (click)="rowReset.emit(row.key)">
                      <span
                        i18n="Button that puts a toggle back to its default">
                        Reset
                      </span>
                    </button>
                  }

                  <app-switch
                    class="shrink-0"
                    [checked]="row.checked"
                    [disabled]="row.isDisabled ?? false"
                    [ariaLabel]="row.ariaLabel"
                    (changed)="
                      rowChanged.emit({ key: row.key, checked: $event })
                    " />
                </div>
              }
            </div>
          }
        </div>
      } @empty {
        <app-empty-state
          compact
          [title]="emptyTitle()"
          [description]="emptyHint">
          <svg emptyStateIcon lucideSearch class="h-8 w-8"></svg>
          <button
            emptyStateAction
            app-stroked-button
            type="button"
            color="neutral"
            size="small"
            (click)="cleared.emit()"
            i18n="Button that clears the search and state filter of a list">
            Clear search and filter
          </button>
        </app-empty-state>
      }
    </div>
  `,
})
export class GroupedToggleListComponent<F extends string = string> {
  readonly groups = input.required<ToggleListGroup[]>();
  readonly filterOptions = input.required<SegmentedOption<F>[]>();
  readonly filtering = input(false);
  readonly searchPlaceholder = input<string | null>(null);
  readonly filterAriaLabel = input<string | null>(null);
  readonly emptyTitle = input.required<string>();

  readonly query = model('');
  readonly filter = model.required<F>();

  readonly rowChanged = output<ToggleListChange>();
  readonly rowReset = output<string>();
  readonly groupAction = output<string>();
  readonly cleared = output();

  protected readonly emptyHint = $localize`:Suggests how to find items after a search found nothing:Try a shorter word, or clear the filter.`;

  protected readonly expanded = signal<ReadonlySet<string>>(new Set());

  protected readonly isAnyGroupOpen = computed(() => {
    const expanded = this.expanded();

    return this.groups().some((group) => expanded.has(group.key));
  });

  protected toggleGroup(key: string) {
    toggleInSet(this.expanded, key);
  }

  protected toggleAllGroups() {
    const keys = this.groups().map((group) => group.key);
    const next = this.isAnyGroupOpen() ? [] : keys;

    this.expanded.set(new Set(next));
  }
}
