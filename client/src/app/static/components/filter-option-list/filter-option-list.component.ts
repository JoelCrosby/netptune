import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  TemplateRef,
  untracked,
  viewChild,
  viewChildren,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LucideCheck } from '@lucide/angular';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

export interface FilterOption<T> {
  value: T;
  label: string;
  hint?: string;
  sticky?: boolean;
}

const nearBottomThresholdPx = 48;

@Component({
  selector: 'app-filter-option-list',
  imports: [
    FilterInputComponent,
    LucideCheck,
    NgTemplateOutlet,
    SpinnerComponent,
  ],
  host: { '[class]': 'hostClass()' },
  template: `
    <app-filter-input
      #search
      appearance="bare"
      [inputRole]="'combobox'"
      [expanded]="true"
      [controls]="listId"
      [keyHint]="dismissKeyHint() ? dismissKeyLabel : null"
      [activeDescendant]="activeOptionId()"
      [value]="query()"
      [placeholder]="searchPlaceholder()"
      [ariaLabel]="searchAriaLabel() ?? searchPlaceholder()"
      (valueChange)="query.set($event)"
      (keydown)="onKeydown($event)">
      <ng-container ngProjectAs="[filterPrefix]">
        <ng-content select="[searchPrefix]" />
      </ng-container>
      <ng-container ngProjectAs="[filterSuffix]">
        <ng-content select="[searchSuffix]" />
      </ng-container>
    </app-filter-input>

    @if (loading()) {
      <div class="flex justify-center p-4">
        <app-spinner diameter="1.5rem" />
      </div>
    } @else {
      <div
        #scroller
        [class]="scrollerClass()"
        role="listbox"
        [id]="listId"
        [attr.aria-multiselectable]="multiple()"
        [attr.aria-label]="listAriaLabel()"
        (scroll)="onScroll($event)">
        @for (
          option of stickyOptions();
          track option.value;
          let index = $index
        ) {
          <div
            #optionRef
            role="option"
            [id]="optionId(index)"
            [class]="optionClass(index, option.value)"
            [attr.aria-selected]="isSelected(option.value)"
            (click)="choose(option.value)"
            (mouseenter)="activeIndex.set(index)">
            <ng-container
              [ngTemplateOutlet]="marker"
              [ngTemplateOutletContext]="{
                $implicit: isSelected(option.value),
              }" />
            @if (optionLeading(); as leading) {
              <ng-container
                [ngTemplateOutlet]="leading"
                [ngTemplateOutletContext]="{ $implicit: option }" />
            }
            <span class="min-w-0 flex-1 truncate">{{ option.label }}</span>
            @if (option.hint; as hint) {
              <span class="text-muted max-w-[45%] truncate text-xs">
                {{ hint }}
              </span>
            }
          </div>
        }

        @if (stickyOptions().length && renderedOptions().length) {
          <div
            class="my-1 border-t border-neutral-200 dark:border-neutral-700"></div>
        }

        @for (
          option of renderedOptions();
          track option.value;
          let index = $index
        ) {
          <div
            #optionRef
            role="option"
            [id]="optionId(index + stickyOptions().length)"
            [class]="optionClass(index + stickyOptions().length, option.value)"
            [attr.aria-selected]="isSelected(option.value)"
            (click)="choose(option.value)"
            (mouseenter)="activeIndex.set(index + stickyOptions().length)">
            <ng-container
              [ngTemplateOutlet]="marker"
              [ngTemplateOutletContext]="{
                $implicit: isSelected(option.value),
              }" />
            @if (optionLeading(); as leading) {
              <ng-container
                [ngTemplateOutlet]="leading"
                [ngTemplateOutletContext]="{ $implicit: option }" />
            }
            <span class="min-w-0 flex-1 truncate">{{ option.label }}</span>
            @if (option.hint; as hint) {
              <span class="text-muted max-w-[45%] truncate text-xs">
                {{ hint }}
              </span>
            }
          </div>
        }

        @if (hiddenCount(); as hidden) {
          <button
            type="button"
            class="text-muted hover:text-foreground w-full cursor-pointer px-3 py-2 text-center text-xs transition-colors"
            (click)="loadMore()">
            {{ moreLabel(hidden) }}
          </button>
        }

        @if (!searchableOptions().length) {
          <ng-content select="[emptyState]">
            <p class="text-muted px-4 py-6 text-center text-sm">
              {{ emptyMessage() }}
            </p>
          </ng-content>
        } @else if (!renderedOptions().length) {
          <p class="text-muted px-4 py-6 text-center text-sm">
            {{ noResultsMessage() }}
          </p>
        }
      </div>
    }

    <ng-template #marker let-selected>
      @if (multiple()) {
        <span
          class="flex h-4 w-4 shrink-0 items-center justify-center rounded-sm border border-neutral-300 dark:border-neutral-600"
          [class.bg-primary]="selected"
          [class.border-primary]="selected">
          @if (selected) {
            <svg lucideCheck class="h-3 w-3 text-white"></svg>
          }
        </span>
      } @else {
        <span class="flex h-4 w-4 shrink-0 items-center justify-center">
          @if (selected) {
            <svg lucideCheck class="h-4 w-4"></svg>
          }
        </span>
      }
    </ng-template>
  `,
})
export class FilterOptionListComponent<T> {
  private readonly injector = inject(Injector);

  readonly options = input.required<readonly FilterOption<T>[]>();
  readonly selected = input<ReadonlySet<T>>(new Set<T>());
  /** Rendered between an option's marker and its label, for an avatar or a swatch. */
  readonly optionLeading = input<TemplateRef<{ $implicit: FilterOption<T> }>>();
  readonly listMaxHeightClass = input('max-h-72');
  /** Widened by lists whose labels are long enough to truncate at the default. */
  readonly widthClass = input('w-64');
  /** Off for a list rendered inline, where escape dismisses nothing. */
  readonly dismissKeyHint = input(true);
  /** Tints the picked rows, for a list whose selection is the point rather than a filter. */
  readonly highlightSelected = input(false);
  readonly multiple = input(true);
  readonly loading = input(false);
  readonly pageSize = input(50);
  /** Mirrors the host dropdown's open state so the list can reset and take focus. */
  readonly open = input(false);
  readonly searchPlaceholder = input(
    $localize`:Placeholder in the box that narrows a filter's options:Search`
  );
  readonly searchAriaLabel = input<string | null>(null);
  readonly listAriaLabel = input<string | null>(null);
  readonly emptyMessage = input(
    $localize`:Shown when a filter has no options at all:No options`
  );
  readonly noResultsMessage = input(
    $localize`:Shown when a filter's search term matches no options:No matches`
  );

  readonly toggled = output<T>();
  readonly dismissed = output();

  readonly listId = `filter-option-list-${crypto.randomUUID()}`;

  protected readonly hostClass = computed(() => {
    return `flex flex-col overflow-hidden rounded-md ${this.widthClass()}`;
  });

  protected readonly scrollerClass = computed(() => {
    return `custom-scroll overflow-y-auto p-1 ${this.listMaxHeightClass()}`;
  });

  protected readonly dismissKeyLabel = $localize`:Keyboard key that closes a filter's option list, shown as a hint:esc`;

  protected readonly query = signal('');

  private readonly search = viewChild<FilterInputComponent>('search');
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private readonly optionElements =
    viewChildren<ElementRef<HTMLElement>>('optionRef');

  /**
   * Snapshot of what was selected when the list opened. Ordering follows this
   * rather than the live selection so rows do not jump as they are ticked.
   */
  private readonly pinned = signal<ReadonlySet<T>>(new Set<T>());

  protected readonly stickyOptions = computed(() => {
    return this.options().filter((option) => option.sticky);
  });

  protected readonly searchableOptions = computed(() => {
    return this.options().filter((option) => !option.sticky);
  });

  protected readonly filteredOptions = computed(() => {
    const query = this.query().trim().toLowerCase();
    const options = this.searchableOptions();
    const pinned = this.pinned();

    const matches = query
      ? options.filter((option) => this.survivesQuery(option, query))
      : options;

    if (!pinned.size) {
      return matches;
    }

    const head = matches.filter((option) => pinned.has(option.value));
    const tail = matches.filter((option) => !pinned.has(option.value));

    return [...head, ...tail];
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

  private readonly navigableCount = computed(() => {
    return this.stickyOptions().length + this.filteredOptions().length;
  });

  // Typing arms Enter on the first match rather than on a sticky entry above it.
  protected readonly activeIndex = linkedSignal<string, number>({
    source: () => this.query(),
    computation: (query) => {
      return query.trim() ? this.stickyOptions().length : -1;
    },
  });

  protected readonly activeOptionId = computed(() => {
    const index = this.activeIndex();
    const outOfRange = index < 0 || index >= this.navigableCount();

    return outOfRange ? null : this.optionId(index);
  });

  constructor() {
    effect(() => {
      const open = this.open();

      untracked(() => {
        if (!open) {
          this.clearSearch();

          return;
        }

        this.pinned.set(new Set(this.selected()));
        this.focusSearch();
      });
    });
  }

  protected optionId(index: number): string {
    return `${this.listId}-option-${index}`;
  }

  protected isSelected(value: T): boolean {
    return this.selected().has(value);
  }

  protected optionClass(index: number, value: T): string {
    const base =
      'flex w-full items-center gap-3 rounded-sm px-3 py-2 text-left text-sm cursor-pointer select-none transition-colors';
    const picked = this.highlightSelected() && this.isSelected(value);

    if (picked) {
      return `${base} bg-primary/25`;
    }

    return this.activeIndex() === index
      ? `${base} bg-neutral-100 dark:bg-neutral-800`
      : `${base} hover:bg-neutral-100 dark:hover:bg-neutral-800`;
  }

  protected moreLabel(hidden: number): string {
    return $localize`:Button that renders the next page of a filter's options. COUNT is how many options remain:Show more (${hidden}:COUNT: remaining)`;
  }

  protected choose(value: T) {
    this.toggled.emit(value);
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

  protected onKeydown(event: KeyboardEvent) {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.moveActive(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActive(-1);
        break;
      case 'Home':
        event.preventDefault();
        this.setActive(0);
        break;
      case 'End':
        event.preventDefault();
        this.setActive(this.navigableCount() - 1);
        break;
      case 'Enter':
        event.preventDefault();
        this.chooseActive();
        break;
      case 'Escape':
        event.preventDefault();
        this.dismissed.emit();
        break;
    }
  }

  private survivesQuery(option: FilterOption<T>, query: string): boolean {
    const matches =
      option.label.toLowerCase().includes(query) ||
      !!option.hint?.toLowerCase().includes(query);

    return matches || this.selected().has(option.value);
  }

  private chooseActive() {
    const index = this.activeIndex();

    if (index < 0) return;

    const sticky = this.stickyOptions();
    const option =
      index < sticky.length
        ? sticky[index]
        : this.filteredOptions()[index - sticky.length];

    if (!option) return;

    this.choose(option.value);
  }

  private moveActive(delta: number) {
    const next = this.activeIndex() + delta;

    this.setActive(next < 0 ? 0 : next);
  }

  private setActive(index: number) {
    const count = this.navigableCount();

    if (!count) return;

    const clamped = Math.max(0, Math.min(index, count - 1));

    this.activeIndex.set(clamped);
    this.revealActive(clamped);
  }

  private revealActive(index: number) {
    const optionsBefore = index - this.stickyOptions().length + 1;
    const pageSize = this.pageSize();
    const requiredPage = Math.ceil(optionsBefore / pageSize);

    if (requiredPage > this.page()) {
      this.page.set(requiredPage);
    }

    afterNextRender(
      () => {
        const element = this.optionElements()[index]?.nativeElement;

        element?.scrollIntoView({ block: 'nearest' });
      },
      { injector: this.injector }
    );
  }

  // Clearing on close rather than on open means the panel never attaches
  // showing the previous session's narrowed list.
  private clearSearch() {
    this.query.set('');
    this.page.set(1);
    this.activeIndex.set(-1);
    this.scroller()?.nativeElement.scrollTo({ top: 0 });
  }

  private focusSearch() {
    this.search()?.focus();

    afterNextRender(() => this.search()?.focus(), { injector: this.injector });
  }
}
