import {
  AfterViewInit,
  Component,
  HostListener,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { LucideSearch } from '@lucide/angular';
import {
  Command,
  CommandRegistry,
} from '@core/services/command-registry.service';
import { SearchService } from '@core/services/search.service';
import { SearchResult } from '@core/models/search-result';
import { delayedLoading } from '@core/util/delayed-loading';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { CommandPaletteService } from './command-palette.service';
import { RecentItem, RecentItemsService } from './recent-items.service';
import { CommandItemComponent } from './command-item.component';
import { RecentItemComponent } from './recent-item.component';
import { SearchResultItemComponent } from './search-result-item.component';

const SEARCH_HINT = $localize`:Command palette hint shown before the user types. The > character is a literal prefix and must not be translated:Type to search or use > for commands.`;
const SEARCH_UNAVAILABLE = $localize`:Shown when the command palette search backend cannot be reached:Search is unavailable right now.`;
const NO_RESULTS = $localize`:Shown when a command palette search matches nothing:No results found.`;

interface PaletteEmptyState {
  hasItems: boolean;
  hasQuery: boolean;
  searching: boolean;
  failed: boolean;
}

type PaletteItem =
  | { kind: 'command'; command: Command }
  | { kind: 'result'; result: SearchResult }
  | { kind: 'recent'; item: RecentItem };

@Component({
  selector: 'app-command-palette',
  imports: [
    FormsModule,
    LucideSearch,
    CommandItemComponent,
    ProgressBarComponent,
    RecentItemComponent,
    SearchResultItemComponent,
  ],
  template: `
    <ng-template #dialogTmpl>
      <div
        class="bg-board-group text-popover-foreground border-border flex w-full flex-col overflow-hidden rounded-md border shadow-md"
        role="dialog"
        aria-modal="true"
        i18n-aria-label="Accessible name of the command palette dialog"
        aria-label="Command Palette">
        <div class="border-border flex items-center border-b px-3">
          <svg lucideSearch class="mr-2 h-4 w-4 shrink-0 opacity-50"></svg>
          <input
            #searchInput
            type="text"
            class="placeholder:text-muted flex h-11 w-full bg-transparent py-3 outline-none disabled:cursor-not-allowed disabled:opacity-50"
            role="combobox"
            aria-expanded="true"
            aria-autocomplete="list"
            aria-controls="command-palette-options"
            [attr.aria-activedescendant]="activeOptionId()"
            [placeholder]="inputPlaceholder()"
            [ngModel]="queryValue()"
            (ngModelChange)="onQueryChange($event)"
            autocomplete="off"
            spellcheck="false" />
        </div>

        <div
          class="h-1 shrink-0"
          [class.invisible]="!delayedSearching()"
          [attr.aria-hidden]="delayedSearching() ? null : 'true'">
          <app-progress-bar mode="indeterminate" [rounded]="false" />
        </div>

        <div
          id="command-palette-options"
          role="listbox"
          i18n-aria-label="Accessible name of the command palette result list"
          aria-label="Results"
          class="max-h-120 overflow-x-hidden overflow-y-auto p-1">
          @if (emptyMessage(); as message) {
            <p class="text-muted py-6 text-center text-sm">{{ message }}</p>
          }

          @if (showRecentGroup()) {
            <div
              class="overflow-hidden p-1"
              role="group"
              i18n-aria-label="Accessible name of the recently-visited group"
              aria-label="Recent">
              <p
                class="text-muted px-2 py-1.5 text-xs font-medium"
                aria-hidden="true">
                <span
                  i18n="
                    Heading above recently-visited items in the command palette
                  "
                  >Recent</span
                >
              </p>
              @for (item of recentItems(); track item.url; let idx = $index) {
                <app-recent-item
                  [item]="item"
                  [optionId]="optionId(idx)"
                  [selected]="selectedIndex() === idx"
                  (activate)="activateRecentItem($event)"
                  (hover)="selectedIndex.set(idx)" />
              }
            </div>
            <div class="bg-border -mx-1 my-1 h-px" aria-hidden="true"></div>
          }

          @if (commandItems().length > 0 && !searchOnlyMode()) {
            <div
              class="overflow-hidden p-1"
              role="group"
              i18n-aria-label="Accessible name of the available-commands group"
              aria-label="Actions">
              <p
                class="text-muted px-2 py-1.5 text-xs font-medium"
                aria-hidden="true">
                <span
                  i18n="Heading above runnable commands in the command palette"
                  >Actions</span
                >
              </p>
              @for (cmd of commandItems(); track cmd.id; let idx = $index) {
                <app-command-item
                  [command]="cmd"
                  [optionId]="optionId(commandOffset() + idx)"
                  [selected]="selectedIndex() === commandOffset() + idx"
                  (activate)="activateCommand($event)"
                  (hover)="selectedIndex.set(commandOffset() + idx)" />
              }
            </div>
          }

          @if (searchResultItems().length > 0 && !commandOnlyMode()) {
            @if (commandItems().length > 0 && !searchOnlyMode()) {
              <div class="bg-border -mx-1 my-1 h-px" aria-hidden="true"></div>
            }
            <div
              class="overflow-hidden p-1"
              role="group"
              i18n-aria-label="Accessible name of the search-results group"
              aria-label="Results">
              <p
                class="text-muted px-2 py-1.5 text-xs font-medium"
                aria-hidden="true">
                <span i18n="Heading above search results in the command palette"
                  >Results</span
                >
              </p>
              @for (
                result of searchResultItems();
                track result.url;
                let idx = $index
              ) {
                <app-search-result-item
                  [result]="result"
                  [optionId]="optionId(searchOffset() + idx)"
                  [selected]="selectedIndex() === searchOffset() + idx"
                  (activate)="activateResult($event)"
                  (hover)="selectedIndex.set(searchOffset() + idx)" />
              }
            </div>
          }
        </div>

        <span class="sr-only" aria-live="polite">{{ announcement() }}</span>
      </div>
    </ng-template>
  `,
})
export class CommandPaletteComponent implements AfterViewInit, OnDestroy {
  private readonly dialogTmpl =
    viewChild.required<TemplateRef<unknown>>('dialogTmpl');

  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private router = inject(Router);
  private registry = inject(CommandRegistry);
  private recentService = inject(RecentItemsService);
  private overlayRef: OverlayRef;
  private portal!: TemplatePortal;

  palette = inject(CommandPaletteService);
  search = inject(SearchService);

  queryValue = signal('');
  selectedIndex = signal(0);

  commandOnlyMode = computed(() => this.queryValue().startsWith('>'));
  searchOnlyMode = computed(
    () => this.queryValue().startsWith('#') || this.queryValue().startsWith('@')
  );

  inputPlaceholder = computed(() => {
    const q = this.queryValue();
    if (q.startsWith('>'))
      return $localize`:Command palette input placeholder while in command mode:Search commands…`;
    if (q.startsWith('#'))
      return $localize`:Command palette input placeholder while in task-search mode:Search tasks…`;
    if (q.startsWith('@'))
      return $localize`:Command palette input placeholder while in project-search mode:Search projects…`;
    return $localize`:Default command palette input placeholder. The > character is a literal prefix and must not be translated:Search or type > for commands…`;
  });

  recentItems = computed(() =>
    !this.queryValue() ? this.recentService.items() : []
  );
  showRecentGroup = computed(
    () => !this.queryValue() && this.recentItems().length > 0
  );

  commandItems = computed<Command[]>(() => {
    const q = this.commandOnlyMode()
      ? this.queryValue().slice(1).trim()
      : this.queryValue().trim();
    return this.registry.filter(q);
  });

  searchResultItems = computed(() => this.search.results());

  commandOffset = computed(() => this.recentItems().length);
  searchOffset = computed(
    () =>
      this.commandOffset() +
      (this.commandOnlyMode() ? 0 : this.commandItems().length)
  );

  activeOptionId = computed(() => {
    if (!this.items().length) {
      return null;
    }

    return this.optionId(this.selectedIndex());
  });

  delayedSearching = delayedLoading(this.search.isSearching);

  emptyMessage = linkedSignal<PaletteEmptyState, string>({
    source: () => ({
      hasItems: this.items().length > 0,
      hasQuery: Boolean(this.queryValue()),
      searching: this.search.isSearching(),
      failed: Boolean(this.search.error()),
    }),
    computation: (state, previous) => {
      if (state.hasItems) return '';
      if (!state.hasQuery) return SEARCH_HINT;
      if (state.failed) return SEARCH_UNAVAILABLE;
      if (state.searching) {
        return previous?.value === NO_RESULTS ? NO_RESULTS : '';
      }

      return NO_RESULTS;
    },
  });

  announcement = computed(() => {
    if (!this.queryValue()) {
      return '';
    }

    if (this.search.isSearching()) {
      return this.delayedSearching()
        ? $localize`:Shown while a command palette search is in flight:Searching…`
        : '';
    }

    if (this.search.error()) {
      return SEARCH_UNAVAILABLE;
    }

    const count = this.items().length;

    return count === 0 ? NO_RESULTS : `${count} results available.`;
  });

  items = computed<PaletteItem[]>(() => {
    const recent: PaletteItem[] = this.recentItems().map((item) => ({
      kind: 'recent' as const,
      item,
    }));
    const commands: PaletteItem[] = this.commandItems().map((command) => ({
      kind: 'command' as const,
      command,
    }));
    const results: PaletteItem[] = this.commandOnlyMode()
      ? []
      : this.searchResultItems().map((result) => ({
          kind: 'result' as const,
          result,
        }));
    return [...recent, ...commands, ...results];
  });

  constructor() {
    this.overlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'np-command-palette-backdrop',
      positionStrategy: this.overlay
        .position()
        .global()
        .centerHorizontally()
        .top('15vh'),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: 'min(calc(100vw - 2rem), 42rem)',
    });

    this.overlayRef.backdropClick().subscribe(() => this.palette.close());

    effect(() => {
      if (this.palette.isOpen()) {
        this.recentService.ensureLoaded();
        if (this.portal && !this.overlayRef.hasAttached()) {
          this.overlayRef.attach(this.portal);
        }
        setTimeout(() => {
          this.queryValue.set('');
          this.selectedIndex.set(0);
          this.search.setQuery('');
          this.overlayRef.overlayElement
            .querySelector<HTMLInputElement>('input')
            ?.focus();
        }, 0);
      } else if (this.overlayRef.hasAttached()) {
        this.overlayRef.detach();
      }
    });
  }

  ngAfterViewInit() {
    this.portal = new TemplatePortal(this.dialogTmpl(), this.vcr);
  }

  optionId(index: number) {
    return `command-palette-option-${index}`;
  }

  ngOnDestroy() {
    this.overlayRef.dispose();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      this.palette.toggle();
      return;
    }

    if (!this.palette.isOpen()) return;

    switch (e.key) {
      case 'Escape':
        this.palette.close();
        break;
      case 'ArrowDown':
        e.preventDefault();
        this.moveSelection(1);
        break;
      case 'ArrowUp':
        e.preventDefault();
        this.moveSelection(-1);
        break;
      case 'Enter':
        e.preventDefault();
        this.activateSelected();
        break;
    }
  }

  onQueryChange(q: string) {
    this.queryValue.set(q);
    this.selectedIndex.set(0);
    const prefix = q[0];
    const stripped =
      prefix === '>' || prefix === '#' || prefix === '@'
        ? q.slice(1).trim()
        : q.trim();

    if (prefix === '>') {
      this.search.setQuery('');
      return;
    }

    if (prefix === '#') {
      this.search.setQuery(stripped, ['tasks']);
      return;
    }

    if (prefix === '@') {
      this.search.setQuery(stripped, ['projects']);
      return;
    }

    this.search.setQuery(stripped);
  }

  moveSelection(delta: number) {
    const total = this.items().length;
    if (total === 0) return;
    this.selectedIndex.update((i) => (i + delta + total) % total);
    this.scrollSelectionIntoView();
  }

  private scrollSelectionIntoView() {
    setTimeout(() => {
      this.overlayRef.overlayElement
        .querySelector<HTMLElement>(`#${this.optionId(this.selectedIndex())}`)
        ?.scrollIntoView({ block: 'nearest' });
    });
  }

  activateSelected() {
    const all = this.items();
    const idx = this.selectedIndex();
    if (idx >= all.length) return;

    const item = all[idx];
    if (item.kind === 'command') this.activateCommand(item.command);
    else if (item.kind === 'result') this.activateResult(item.result);
    else if (item.kind === 'recent') this.activateRecentItem(item.item);
  }

  activateCommand(cmd: Command) {
    this.palette.close();
    cmd.execute();
  }

  activateResult(result: SearchResult) {
    this.palette.close();
    this.recentService.addRecent({
      title: result.title,
      url: result.url,
      type: result.type,
      entityId: result.id.toString(),
    });
    void this.router.navigateByUrl(result.url);
  }

  activateRecentItem(item: RecentItem) {
    this.palette.close();
    void this.router.navigateByUrl(item.url);
  }
}
